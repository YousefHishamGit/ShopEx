using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class SupplierInvoiceRepository : ISupplierInvoiceRepository
    {
        private readonly ShopContext _db;

        public SupplierInvoiceRepository(ShopContext db)
        {
            _db = db;
        }
        public Task<SupplierInvoice?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.SupplierInvoices
                      .Include(i => i.Items)
                      .Include(i => i.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(i => i.ID == id, ct);
        }
        public Task<List<SupplierInvoice>> GetAllAsync(CancellationToken ct = default)
        {
            return _db.SupplierInvoices
                      .Include(i => i.Items)
                      .Include(i => i.Payments)
                      .AsSplitQuery()
                      .OrderByDescending(i => i.ID)
                      .ToListAsync(ct);
        }
        public async Task<SupplierInvoicesPage> GetPagedAsync(
            int page, int pageSize, string? search, IReadOnlyCollection<int>? supplierIdsMatchingSearch,
            string? status, int? supplierId, CancellationToken ct = default)
        {
            var q = _db.SupplierInvoices.AsNoTracking()
                .Select(i => new
                {
                    Invoice = i,
                    Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
                    ItemsCount = i.Items.Count,
                    PaymentsCount = i.Payments.Count
                });

            if (supplierId.HasValue)
                q = q.Where(x => x.Invoice.RMSSupplierID == supplierId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var ids = supplierIdsMatchingSearch ?? Array.Empty<int>();
                q = q.Where(x => x.Invoice.InvoiceNumber.Contains(term) || ids.Contains(x.Invoice.RMSSupplierID));
            }

            q = (status ?? string.Empty).ToLowerInvariant() switch
            {
                "paid" => q.Where(x => x.Invoice.TotalAmount - x.Paid <= 0),
                "partiallypaid" or "partial" => q.Where(x => x.Invoice.TotalAmount - x.Paid > 0 && x.Paid > 0),
                "unpaid" => q.Where(x => x.Invoice.TotalAmount - x.Paid > 0 && x.Paid <= 0),
                _ => q
            };

            // Totals over the whole filtered set, in a single SQL aggregate
            var totals = await q.GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    Total = g.Sum(x => x.Invoice.TotalAmount),
                    Paid = g.Sum(x => x.Paid)
                })
                .FirstOrDefaultAsync(ct);

            var rows = await q.OrderByDescending(x => x.Invoice.InvoiceDate).ThenByDescending(x => x.Invoice.ID)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(ct);

            var count = totals?.Count ?? 0;
            return new SupplierInvoicesPage
            {
                TotalCount = count,
                PageNumber = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)count / pageSize)),
                TotalAmount = totals?.Total ?? 0m,
                TotalPaid = totals?.Paid ?? 0m,
                TotalRemaining = (totals?.Total ?? 0m) - (totals?.Paid ?? 0m),
                Items = rows.Select(x => new SupplierInvoiceSummaryResponse
                {
                    ID = x.Invoice.ID,
                    RMSSupplierID = x.Invoice.RMSSupplierID,
                    InvoiceNumber = x.Invoice.InvoiceNumber,
                    InvoiceDate = x.Invoice.InvoiceDate,
                    DiscountAmount = x.Invoice.DiscountAmount,
                    TotalAmount = x.Invoice.TotalAmount,
                    SupplierInvoiceStatus = x.Invoice.SupplierInvoiceStatus,
                    PaymentType = x.Invoice.PaymentType,
                    CreditDays = x.Invoice.CreditDays,
                    DueDate = x.Invoice.DueDate,
                    PaidAmount = x.Paid,
                    RemainingAmount = x.Invoice.TotalAmount - x.Paid,
                    ItemsCount = x.ItemsCount,
                    PaymentsCount = x.PaymentsCount
                }).ToList()
            };
        }

        public Task<bool> ExistsAsync(int rmsSupplierId, string invoiceNumber, CancellationToken ct = default)
        {
            return _db.SupplierInvoices
                      .AnyAsync(i => i.RMSSupplierID == rmsSupplierId
                                  && i.InvoiceNumber == invoiceNumber, ct);
        }
        public async Task AddAsync(SupplierInvoice invoice, CancellationToken ct = default)
        {
            await _db.SupplierInvoices.AddAsync(invoice, ct);
        }
    }
}

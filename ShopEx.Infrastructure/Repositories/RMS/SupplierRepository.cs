using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Domain.RMSEntity;
using ShopEx.Infrastructure.Persistence.RMS;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly RMSContext _context;
        private readonly ShopContext _shopContext;

        public SupplierRepository(RMSContext context, ShopContext shopContext)
        {
            _context = context;
            _shopContext = shopContext;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers.AsNoTracking().ToListAsync();
        }

        public async Task<Supplier> GetByIdAsync(int id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<IEnumerable<SupplierInvoice>> GetInvoicesBySupplierIdAsync(int rmssupplierId)
        {
            return await _shopContext.SupplierInvoices
                .AsNoTracking()
                .Where(i => i.RMSSupplierID == rmssupplierId)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .AsSplitQuery()
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }

        // ---------- Suppliers list (SQL paging) ----------

        private IQueryable<Supplier> FilterSuppliers(string? search)
        {
            IQueryable<Supplier> q = _context.Suppliers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(s => s.SupplierName.Contains(term) || s.Code.Contains(term));
            }
            return q;
        }

        public async Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize)
        {
            var q = FilterSuppliers(search);
            var total = await q.CountAsync();
            var items = await q.OrderBy(s => s.SupplierName).ThenBy(s => s.ID)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync();
            return (items, total);
        }

        public async Task<Dictionary<int, Supplier>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return new Dictionary<int, Supplier>();
            return await _context.Suppliers.AsNoTracking()
                                 .Where(s => list.Contains(s.ID))
                                 .ToDictionaryAsync(s => s.ID);
        }

        public Task<List<int>> SearchIdsAsync(string term)
        {
            term = term.Trim();
            return _context.Suppliers.AsNoTracking()
                           .Where(s => s.SupplierName.Contains(term) || s.Code.Contains(term))
                           .Select(s => s.ID)
                           .Take(500)
                           .ToListAsync();
        }

        // ---------- Statement ----------

        public async Task<SupplierAccountSummary> GetAccountSummaryAsync(int supplierId)
        {
            // Narrow projections only: no item / payment rows are loaded
            var invoices = await _shopContext.SupplierInvoices.AsNoTracking()
                .Where(i => i.RMSSupplierID == supplierId)
                .Select(i => new { i.TotalAmount, Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
                .ToListAsync();

            var returns = await _shopContext.SupplierReturns.AsNoTracking()
                .Where(r => r.RMSSupplierID == supplierId && r.Status == SupplierReturnStatus.Confirmed)
                .Select(r => new
                {
                    Total = r.Items.Sum(i => (decimal?)(i.Quantity * i.UnitPrice)) ?? 0m,
                    Refunded = r.Refunds.Sum(x => (decimal?)x.Amount) ?? 0m
                })
                .ToListAsync();

            return new SupplierAccountSummary
            {
                InvoicesCount = invoices.Count,
                TotalInvoices = invoices.Sum(i => i.TotalAmount),
                TotalPaid = invoices.Sum(i => i.Paid),
                PaidCount = invoices.Count(i => i.TotalAmount - i.Paid <= 0),
                PartialCount = invoices.Count(i => i.TotalAmount - i.Paid > 0 && i.Paid > 0),
                UnpaidCount = invoices.Count(i => i.TotalAmount - i.Paid > 0 && i.Paid <= 0),
                ReturnsCount = await _shopContext.SupplierReturns.CountAsync(r => r.RMSSupplierID == supplierId),
                TotalReturns = returns.Sum(r => r.Total),
                TotalRefunds = returns.Sum(r => r.Refunded),
                TotalPendingRefunds = returns.Sum(r => Math.Max(r.Total - r.Refunded, 0m))
            };
        }

        public async Task<(List<SupplierInvoice> Items, int TotalCount)> GetInvoicesPageAsync(
            int supplierId, string bucket, string? search, int page, int pageSize)
        {
            var q = _shopContext.SupplierInvoices.AsNoTracking()
                .Where(i => i.RMSSupplierID == supplierId)
                .Select(i => new { Invoice = i, Paid = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m });

            q = bucket switch
            {
                "paid" => q.Where(x => x.Invoice.TotalAmount - x.Paid <= 0),
                "partial" => q.Where(x => x.Invoice.TotalAmount - x.Paid > 0 && x.Paid > 0),
                "unpaid" => q.Where(x => x.Invoice.TotalAmount - x.Paid > 0 && x.Paid <= 0),
                _ => q
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(x => x.Invoice.InvoiceNumber.Contains(term));
            }

            var total = await q.CountAsync();
            var ids = await q.OrderByDescending(x => x.Invoice.InvoiceDate).ThenByDescending(x => x.Invoice.ID)
                             .Skip((page - 1) * pageSize).Take(pageSize)
                             .Select(x => x.Invoice.ID)
                             .ToListAsync();

            // Items + payments only for the rows of this page
            var items = await _shopContext.SupplierInvoices.AsNoTracking()
                .Where(i => ids.Contains(i.ID))
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .AsSplitQuery()
                .ToListAsync();

            var order = ids.Select((id, idx) => (id, idx)).ToDictionary(t => t.id, t => t.idx);
            items = items.OrderBy(i => order[i.ID]).ToList();
            return (items, total);
        }

        public async Task<(List<SupplierReturn> Items, int TotalCount)> GetReturnsPageAsync(
            int supplierId, string? search, int page, int pageSize)
        {
            var q = _shopContext.SupplierReturns.AsNoTracking().Where(r => r.RMSSupplierID == supplierId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(r => r.ReturnNumber.Contains(term) || r.Warehouse.Name.Contains(term));
            }

            var total = await q.CountAsync();
            var items = await q.OrderByDescending(r => r.ReturnDate).ThenByDescending(r => r.ID)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .Include(r => r.Warehouse)
                               .Include(r => r.Items)
                               .Include(r => r.Refunds)
                               .AsSplitQuery()
                               .ToListAsync();
            return (items, total);
        }
    }
}

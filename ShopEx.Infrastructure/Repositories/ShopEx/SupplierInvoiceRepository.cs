using Microsoft.EntityFrameworkCore;
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

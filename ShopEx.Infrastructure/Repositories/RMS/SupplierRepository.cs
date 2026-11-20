using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.RMSEntity;
using ShopEx.Infrastructure.Persistence.RMS;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly RMSContext _context;
        private readonly ShopContext _shopContext;

        public SupplierRepository(RMSContext context,ShopContext shopContext)
        {
            _context = context;
            _shopContext = shopContext;
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers.ToListAsync();
        }

        public async Task<Supplier> GetByIdAsync(int id)
        {
            return await _context.Suppliers.FindAsync(id);
        }

        public async Task<IEnumerable<SupplierInvoice>> GetInvoicesBySupplierIdAsync(int rmssupplierId)
        {
            return await _shopContext.SupplierInvoices
                .Where(i => i.RMSSupplierID == rmssupplierId)
                .Include(i => i.Items)
                .Include(i => i.Payments)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync();
        }
    }
}

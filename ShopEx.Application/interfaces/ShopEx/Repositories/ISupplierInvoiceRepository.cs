using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface ISupplierInvoiceRepository
    {
        public Task<SupplierInvoice?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<SupplierInvoice>> GetAllAsync(CancellationToken ct = default);
        Task<bool> ExistsAsync(int rmsSupplierId, string invoiceNumber, CancellationToken ct = default);
        Task AddAsync(SupplierInvoice invoice, CancellationToken ct = default);
    }
}

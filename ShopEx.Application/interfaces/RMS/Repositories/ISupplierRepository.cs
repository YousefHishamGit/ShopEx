using ShopEx.Domain.Entity;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface ISupplierRepository
    {
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier> GetByIdAsync(int id);
        Task<IEnumerable<SupplierInvoice>> GetInvoicesBySupplierIdAsync(int rmssupplierId);
    }
}

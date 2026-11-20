using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Service
{
    public interface ISupplierService
    {
        Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync();
        Task<IEnumerable<ItemResponse>> GetItemsBySupplierAsync(int supplierId);
        Task<SupplierStatementDto?> GetSupplierStatementAsync(int supplierId);
    }
}

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
        Task<(List<SupplierResponse> Items, int TotalCount)> GetSuppliersPagedAsync(string? search, int page, int pageSize);
        Task<IEnumerable<ItemResponse>> GetItemsBySupplierAsync(int supplierId);
        Task<SupplierStatementDto?> GetSupplierStatementAsync(
            int supplierId,
            string? status = null,
            string? section = null,
            int page = 1,
            int pageSize = 10,
            string? search = null);
    }
}

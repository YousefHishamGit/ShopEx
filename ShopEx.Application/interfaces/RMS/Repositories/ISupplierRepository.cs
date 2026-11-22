using ShopEx.Domain.Entity;
using ShopEx.Domain.RMSEntity;
using ShopEx.Application.DTOs.ShopEx;
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
        Task<(List<Supplier> Items, int TotalCount)> GetPagedAsync(string? search, int page, int pageSize);
        Task<Dictionary<int, Supplier>> GetByIdsAsync(IEnumerable<int> ids);
        Task<List<int>> SearchIdsAsync(string term);

        // Statement queries (all filtering/paging done in SQL)
        Task<SupplierAccountSummary> GetAccountSummaryAsync(int supplierId);
        Task<(List<SupplierInvoice> Items, int TotalCount)> GetInvoicesPageAsync(int supplierId, string bucket, string? search, int page, int pageSize);
        Task<(List<SupplierReturn> Items, int TotalCount)> GetReturnsPageAsync(int supplierId, string? search, int page, int pageSize);
    }
}

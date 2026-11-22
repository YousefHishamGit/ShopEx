using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface IItemRepository
    {
        Task<IEnumerable<Item>> GetItemsBySupplierIdAsync(int supplierId);
        Task<Item?> GetByIdAsync(int id);
        Task<Dictionary<int, Item>> GetByIdsAsync(IEnumerable<int> ids);
        Task<Item?> GetByLookupCodeAsync(string lookupCode);
        Task<IEnumerable<Item>> SearchItemsAsync(string? query, int limit = 50);
        Task<List<int>> SearchIdsAsync(string term, int limit = 1000);
    }
}

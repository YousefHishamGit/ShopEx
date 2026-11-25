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
        /// <summary>Retail price per item id (items without a price are returned as 0). Reads only ID and Price, in chunks.</summary>
        Task<Dictionary<int, decimal>> GetPricesByIdsAsync(IEnumerable<int> ids);
        Task<Item?> GetByLookupCodeAsync(string lookupCode);
        Task<IEnumerable<Item>> SearchItemsAsync(string? query, int limit = 50);
        Task<List<int>> SearchIdsAsync(string term, int limit = 1000);
        /// <summary>
        /// The ids from <paramref name="candidateIds"/> whose description / lookup code / id match <paramref name="term"/>.
        /// No result cap: every candidate is checked (in chunks), so searches inside a known set are never truncated.
        /// </summary>
        Task<List<int>> FilterIdsAsync(string term, IEnumerable<int> candidateIds, CancellationToken ct = default);
    }
}

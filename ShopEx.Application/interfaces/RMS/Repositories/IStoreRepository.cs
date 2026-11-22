using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface IStoreRepository
    {
        Task<bool> ExistsAsync(int storeId, CancellationToken ct = default);
        Task<Dictionary<int, string>> GetNamesAsync(CancellationToken ct = default);
    }
}

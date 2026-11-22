using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Infrastructure.Persistence.RMS;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class StoreRepository : IStoreRepository
    {
        private readonly RMSContext _db;

        public StoreRepository(RMSContext db)
        {
            _db = db;
        }

        public Task<bool> ExistsAsync(int storeId, CancellationToken ct = default)
            => _db.Stores.AsNoTracking().AnyAsync(s => s.ID == storeId, ct);

        public async Task<Dictionary<int, string>> GetNamesAsync(CancellationToken ct = default)
        {
            var rows = await _db.Stores.AsNoTracking()
                                .Select(s => new { s.ID, s.Name })
                                .ToListAsync(ct);

            return rows.ToDictionary(s => s.ID, s => s.Name ?? $"فرع #{s.ID}");
        }
    }
}

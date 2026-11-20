using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class BalanceRepository : IBalanceRepository
    {
        private readonly ShopContext _db;

        public BalanceRepository(ShopContext db)
        {
            _db = db;
        }

        // ================================================================
        // Charges
        // ================================================================

        public Task<List<Balance>> GetChargesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var query = _db.Balances
                           .Include(b => b.Closures)
                           .AsQueryable();

            if (storeId.HasValue)
                query = query.Where(b => b.StoreId == storeId.Value);

            if (balanceType.HasValue)
                query = query.Where(b => b.BalanceType == balanceType.Value);

            if (fromDate.HasValue)
                query = query.Where(b => b.ChargedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.ChargedAt <= toDate.Value);

            return query
                .OrderByDescending(b => b.ChargedAt)
                .ToListAsync(ct);
        }

        public Task<Balance?> GetChargeByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Balances
                      .Include(b => b.Closures)
                      .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task AddChargeAsync(Balance balance, CancellationToken ct = default)
        {
            await _db.Balances.AddAsync(balance, ct);
        }

        // ================================================================
        // Closures
        // ================================================================

        public Task<List<BalanceClosure>> GetClosuresByBalanceIdAsync(int balanceId, CancellationToken ct = default)
        {
            return _db.BalanceClosures
                      .Where(c => c.BalanceId == balanceId)
                      .OrderByDescending(c => c.ClosedAt)
                      .ToListAsync(ct);
        }

        public async Task AddClosureAsync(BalanceClosure closure, CancellationToken ct = default)
        {
            await _db.BalanceClosures.AddAsync(closure, ct);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Balance;
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

        private IQueryable<Balance> FilteredCharges(
            int? storeId, BalanceType? balanceType, DateTime? fromDate, DateTime? toDate)
        {
            var query = _db.Balances.AsNoTracking().AsQueryable();

            if (storeId.HasValue)
                query = query.Where(b => b.StoreId == storeId.Value);
            if (balanceType.HasValue)
                query = query.Where(b => b.BalanceType == balanceType.Value);
            if (fromDate.HasValue)
                query = query.Where(b => b.ChargedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.ChargedAt <= toDate.Value);

            return query;
        }

        public Task<List<BalanceSummaryResponse>> GetChargeSummariesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            return FilteredCharges(storeId, balanceType, fromDate, toDate)
                .OrderByDescending(b => b.ChargedAt)
                .Select(b => new BalanceSummaryResponse
                {
                    Id = b.Id,
                    StoreId = b.StoreId,
                    BalanceType = b.BalanceType,
                    Amount = b.Amount,
                    ChargedAt = b.ChargedAt,
                    Notes = b.Notes,
                    TotalUsedAmount = b.Closures.Sum(c => (decimal?)c.AmountUsed) ?? 0m,
                    RemainingAmount = b.Amount - (b.Closures.Sum(c => (decimal?)c.AmountUsed) ?? 0m),
                    ClosuresCount = b.Closures.Count
                })
                .ToListAsync(ct);
        }

        public Task<Balance?> GetChargeByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Balances
                      .AsNoTracking()
                      .Include(b => b.Closures)
                      .FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public Task<List<OpenChargeRow>> GetOpenChargesAsync(
            int storeId, BalanceType balanceType, CancellationToken ct = default)
        {
            return _db.Balances
                .AsNoTracking()
                .Where(b => b.StoreId == storeId && b.BalanceType == balanceType)
                .Select(b => new
                {
                    b.Id,
                    b.ChargedAt,
                    Remaining = b.Amount - (b.Closures.Sum(c => (decimal?)c.AmountUsed) ?? 0m)
                })
                .Where(x => x.Remaining > 0)
                .OrderBy(x => x.ChargedAt).ThenBy(x => x.Id)
                .Select(x => new OpenChargeRow { Id = x.Id, Remaining = x.Remaining })
                .ToListAsync(ct);
        }

        public async Task<decimal> GetUsedAmountAsync(int balanceId, CancellationToken ct = default)
        {
            return await _db.BalanceClosures
                .Where(c => c.BalanceId == balanceId)
                .SumAsync(c => (decimal?)c.AmountUsed, ct) ?? 0m;
        }

        public async Task AddChargeAsync(Balance balance, CancellationToken ct = default)
        {
            await _db.Balances.AddAsync(balance, ct);
        }

        // ================================================================
        // Aggregates / Ledger
        // ================================================================

        public async Task<List<StoreBalanceGroupDto>> GetGroupedAsync(
            int? storeId, BalanceType? balanceType, CancellationToken ct = default)
        {
            var charges = _db.Balances.AsNoTracking().AsQueryable();
            var closures = _db.BalanceClosures.AsNoTracking().AsQueryable();

            if (storeId.HasValue)
            {
                charges = charges.Where(b => b.StoreId == storeId.Value);
                closures = closures.Where(c => c.Balance.StoreId == storeId.Value);
            }
            if (balanceType.HasValue)
            {
                charges = charges.Where(b => b.BalanceType == balanceType.Value);
                closures = closures.Where(c => c.Balance.BalanceType == balanceType.Value);
            }

            var chargeAgg = await charges
                .GroupBy(b => new { b.StoreId, b.BalanceType })
                .Select(g => new
                {
                    g.Key.StoreId,
                    g.Key.BalanceType,
                    Total = g.Sum(x => x.Amount),
                    Count = g.Count(),
                    Last = g.Max(x => (DateTime?)x.ChargedAt)
                })
                .ToListAsync(ct);

            var closureAgg = await closures
                .GroupBy(c => new { c.Balance.StoreId, c.Balance.BalanceType })
                .Select(g => new
                {
                    g.Key.StoreId,
                    g.Key.BalanceType,
                    Total = g.Sum(x => x.AmountUsed),
                    Profit = g.Sum(x => x.Profit),
                    Count = g.Count(),
                    Last = g.Max(x => (DateTime?)x.ClosedAt)
                })
                .ToListAsync(ct);

            var closureLookup = closureAgg.ToDictionary(x => (x.StoreId, x.BalanceType));

            return chargeAgg
                .Select(c =>
                {
                    closureLookup.TryGetValue((c.StoreId, c.BalanceType), out var cl);
                    return new StoreBalanceGroupDto
                    {
                        StoreId = c.StoreId,
                        BalanceType = c.BalanceType,
                        TotalCharged = c.Total,
                        ChargesCount = c.Count,
                        LastChargedAt = c.Last,
                        TotalUsed = cl?.Total ?? 0m,
                        TotalProfit = cl?.Profit ?? 0m,
                        ClosuresCount = cl?.Count ?? 0,
                        LastClosedAt = cl?.Last
                    };
                })
                .OrderBy(x => x.StoreId)
                .ThenBy(x => x.BalanceType)
                .ToList();
        }

        public Task<List<BalanceTransactionItemDto>> GetLatestTransactionsAsync(
            int? storeId,
            BalanceType? balanceType,
            DateTime? fromDate,
            DateTime? toDate,
            int take,
            CancellationToken ct = default)
        {
            var charges = FilteredCharges(storeId, balanceType, fromDate, toDate)
                .Select(b => new BalanceTransactionItemDto
                {
                    Id = b.Id,
                    TransactionType = "Charge",
                    StoreId = b.StoreId,
                    BalanceType = b.BalanceType,
                    Amount = b.Amount,
                    TransactionDate = b.ChargedAt,
                    Notes = b.Notes,
                    BalanceId = b.Id
                });

            var closureQuery = _db.BalanceClosures.AsNoTracking().AsQueryable();
            if (storeId.HasValue)
                closureQuery = closureQuery.Where(c => c.Balance.StoreId == storeId.Value);
            if (balanceType.HasValue)
                closureQuery = closureQuery.Where(c => c.Balance.BalanceType == balanceType.Value);
            if (fromDate.HasValue)
                closureQuery = closureQuery.Where(c => c.ClosedAt >= fromDate.Value);
            if (toDate.HasValue)
                closureQuery = closureQuery.Where(c => c.ClosedAt <= toDate.Value);

            var closures = closureQuery
                .Select(c => new BalanceTransactionItemDto
                {
                    Id = c.Id,
                    TransactionType = "Closure",
                    StoreId = c.Balance.StoreId,
                    BalanceType = c.Balance.BalanceType,
                    Amount = c.AmountUsed,
                    Profit = c.Profit,
                    TransactionDate = c.ClosedAt,
                    Notes = c.Notes,
                    BalanceId = c.BalanceId
                });

            return charges
                .Concat(closures)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Take(take)
                .ToListAsync(ct);
        }

        // ================================================================
        // Closures
        // ================================================================

        public Task<List<BalanceClosure>> GetClosuresByBalanceIdAsync(int balanceId, CancellationToken ct = default)
        {
            return _db.BalanceClosures
                      .AsNoTracking()
                      .Where(c => c.BalanceId == balanceId)
                      .OrderByDescending(c => c.ClosedAt)
                      .ToListAsync(ct);
        }

        public async Task AddClosureAsync(BalanceClosure closure, CancellationToken ct = default)
        {
            await _db.BalanceClosures.AddAsync(closure, ct);
        }

        public async Task AcquireStoreLockAsync(int storeId, BalanceType balanceType, CancellationToken ct = default)
        {
            // Transaction-scoped application lock: released automatically on commit/rollback.
            var resource = $"Balance:{storeId}:{(int)balanceType}";
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000",
                ct);
        }
    }
}

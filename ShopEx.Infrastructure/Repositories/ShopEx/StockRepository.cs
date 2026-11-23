using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class StockRepository : IStockRepository
    {
        private readonly ShopContext _db;

        public StockRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<StockBalance?> GetBalanceAsync(int warehouseId, int rmsItemId, CancellationToken ct = default)
        {
            return _db.StockBalances
                      .Include(b => b.Warehouse)
                      .FirstOrDefaultAsync(b => b.WarehouseID == warehouseId && b.RMSItemID == rmsItemId, ct);
        }

        public async Task<(List<StockBalance> Items, int TotalCount)> GetInStockBalancesPagedAsync(
            int warehouseId, IReadOnlyCollection<int>? itemIds, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.StockBalances
                           .AsNoTracking()
                           .Where(b => b.WarehouseID == warehouseId && b.QuantityOnHand > 0);

            if (itemIds != null)
            {
                query = query.Where(b => itemIds.Contains(b.RMSItemID));
            }

            var total = await query.CountAsync(ct);
            var items = await query.OrderBy(b => b.RMSItemID)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync(ct);
            return (items, total);
        }

        public async Task<(decimal TotalQuantity, decimal TotalCost)> GetInStockTotalsAsync(int warehouseId, CancellationToken ct = default)
        {
            var totals = await _db.StockBalances
                .AsNoTracking()
                .Where(b => b.WarehouseID == warehouseId && b.QuantityOnHand > 0)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Qty = g.Sum(b => b.QuantityOnHand),
                    Cost = g.Sum(b => b.QuantityOnHand * b.AverageCost)
                })
                .FirstOrDefaultAsync(ct);

            return totals == null ? (0m, 0m) : (totals.Qty, totals.Cost);
        }

        public async Task<List<(int RMSItemID, decimal Quantity)>> GetInStockQuantitiesAsync(int warehouseId, CancellationToken ct = default)
        {
            var rows = await _db.StockBalances
                .AsNoTracking()
                .Where(b => b.WarehouseID == warehouseId && b.QuantityOnHand > 0)
                .Select(b => new { b.RMSItemID, b.QuantityOnHand })
                .ToListAsync(ct);

            return rows.Select(r => (r.RMSItemID, r.QuantityOnHand)).ToList();
        }

        public Task<List<StockBalance>> GetBalancesByItemAsync(int rmsItemId, CancellationToken ct = default)
        {
            return _db.StockBalances
                      .Include(b => b.Warehouse)
                      .Where(b => b.RMSItemID == rmsItemId)
                      .OrderBy(b => b.Warehouse.Name)
                      .ToListAsync(ct);
        }

        public Task<List<StockMovement>> GetRecentMovementsAsync(int? warehouseId, int take, CancellationToken ct = default)
        {
            IQueryable<StockMovement> query = _db.StockMovements.AsNoTracking().Include(m => m.Warehouse);

            if (warehouseId.HasValue)
            {
                query = query.Where(m => m.WarehouseID == warehouseId.Value);
            }

            return query.OrderByDescending(m => m.MovementDate)
                        .ThenByDescending(m => m.ID)
                        .Take(take)
                        .ToListAsync(ct);
        }

        public async Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
        {
            await _db.StockMovements.AddAsync(movement, ct);
        }

        public async Task AddBalanceAsync(StockBalance balance, CancellationToken ct = default)
        {
            await _db.StockBalances.AddAsync(balance, ct);
        }

        public void UpdateBalance(StockBalance balance)
        {
            _db.StockBalances.Update(balance);
        }
    }
}

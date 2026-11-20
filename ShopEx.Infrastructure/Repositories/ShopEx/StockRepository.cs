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

        public Task<List<StockBalance>> GetBalancesByWarehouseAsync(int warehouseId, bool lowStockOnly = false, CancellationToken ct = default)
        {
            var query = _db.StockBalances
                           .Include(b => b.Warehouse)
                           .Where(b => b.WarehouseID == warehouseId);

            if (lowStockOnly)
            {
                query = query.Where(b => b.ReorderLevel.HasValue && b.QuantityOnHand <= b.ReorderLevel.Value);
            }

            return query.OrderBy(b => b.RMSItemID).ToListAsync(ct);
        }

        public Task<List<StockBalance>> GetBalancesByItemAsync(int rmsItemId, CancellationToken ct = default)
        {
            return _db.StockBalances
                      .Include(b => b.Warehouse)
                      .Where(b => b.RMSItemID == rmsItemId)
                      .OrderBy(b => b.Warehouse.Name)
                      .ToListAsync(ct);
        }

        public Task<List<StockMovement>> GetMovementsAsync(
            int? warehouseId = null,
            int? rmsItemId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            IQueryable<StockMovement> query = _db.StockMovements.Include(m => m.Warehouse);

            if (warehouseId.HasValue)
            {
                query = query.Where(m => m.WarehouseID == warehouseId.Value);
            }

            if (rmsItemId.HasValue)
            {
                query = query.Where(m => m.RMSItemID == rmsItemId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(m => m.MovementDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(m => m.MovementDate <= toDate.Value);
            }

            return query.OrderByDescending(m => m.MovementDate)
                        .ThenByDescending(m => m.ID)
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

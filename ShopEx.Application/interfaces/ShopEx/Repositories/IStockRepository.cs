using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IStockRepository
    {
        Task<StockBalance?> GetBalanceAsync(int warehouseId, int rmsItemId, CancellationToken ct = default);
        Task<List<StockBalance>> GetBalancesByWarehouseAsync(int warehouseId, bool lowStockOnly = false, CancellationToken ct = default);
        Task<List<StockBalance>> GetBalancesByItemAsync(int rmsItemId, CancellationToken ct = default);
        Task<List<StockMovement>> GetMovementsAsync(int? warehouseId = null, int? rmsItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
        Task AddMovementAsync(StockMovement movement, CancellationToken ct = default);
        Task AddBalanceAsync(StockBalance balance, CancellationToken ct = default);
        void UpdateBalance(StockBalance balance);
    }
}

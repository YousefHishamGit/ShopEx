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
        /// <summary>Balances with QuantityOnHand &gt; 0, paged in the database. Optional filter by item ids.</summary>
        Task<(List<StockBalance> Items, int TotalCount)> GetInStockBalancesPagedAsync(int warehouseId, IReadOnlyCollection<int>? itemIds, int page, int pageSize, CancellationToken ct = default);
        /// <summary>Warehouse-wide totals over in-stock rows: sum of quantities and sum of quantity x average cost.</summary>
        Task<(decimal TotalQuantity, decimal TotalCost)> GetInStockTotalsAsync(int warehouseId, CancellationToken ct = default);
        Task<List<StockBalance>> GetBalancesByItemAsync(int rmsItemId, CancellationToken ct = default);
        /// <summary>Latest movements, newest first, limited to <paramref name="take"/> rows.</summary>
        Task<List<StockMovement>> GetRecentMovementsAsync(int? warehouseId, int take, CancellationToken ct = default);
        Task AddMovementAsync(StockMovement movement, CancellationToken ct = default);
        Task AddBalanceAsync(StockBalance balance, CancellationToken ct = default);
        void UpdateBalance(StockBalance balance);
    }
}

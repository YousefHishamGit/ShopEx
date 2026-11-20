using ShopEx.Application.DTOs.Stock;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IStockService
    {
        // Warehouse Management
        Task<List<WarehouseResponse>> GetWarehousesAsync(bool activeOnly = false, CancellationToken ct = default);
        Task<WarehouseResponse?> GetWarehouseByIdAsync(int id, CancellationToken ct = default);
        Task<WarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default);
        Task<WarehouseResponse> UpdateWarehouseAsync(int id, UpdateWarehouseRequest request, CancellationToken ct = default);

        // Stock Balances & Movements
        Task<List<StockBalanceResponse>> GetBalancesAsync(int warehouseId, bool lowStockOnly = false, CancellationToken ct = default);
        Task<StockBalanceResponse?> GetItemBalanceAsync(int warehouseId, int rmsItemId, CancellationToken ct = default);
        Task<List<StockMovementResponse>> GetMovementsAsync(int? warehouseId = null, int? rmsItemId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default);
        Task<StockBalanceResponse> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken ct = default);
    }
}

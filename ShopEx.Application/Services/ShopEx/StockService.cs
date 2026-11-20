using ShopEx.Application.DTOs.Stock;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class StockService : IStockService
    {
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockService(
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IUnitOfWork unitOfWork)
        {
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // Warehouse Management
        public async Task<List<WarehouseResponse>> GetWarehousesAsync(bool activeOnly = false, CancellationToken ct = default)
        {
            var warehouses = await _warehouseRepository.GetAllAsync(activeOnly, ct);
            return warehouses.Select(w => new WarehouseResponse
            {
                ID = w.ID,
                Name = w.Name,
                Location = w.Location,
                IsDefault = w.IsDefault,
                IsActive = w.IsActive,
                TotalItemsCount = w.Balances?.Count ?? 0
            }).ToList();
        }

        public async Task<WarehouseResponse?> GetWarehouseByIdAsync(int id, CancellationToken ct = default)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(id, ct);
            if (warehouse == null) return null;

            return new WarehouseResponse
            {
                ID = warehouse.ID,
                Name = warehouse.Name,
                Location = warehouse.Location,
                IsDefault = warehouse.IsDefault,
                IsActive = warehouse.IsActive,
                TotalItemsCount = warehouse.Balances?.Count ?? 0
            };
        }

        public async Task<WarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("اسم المخزن مطلوب.", nameof(request.Name));
            }

            if (await _warehouseRepository.ExistsByNameAsync(name, null, ct))
            {
                throw new InvalidOperationException($"يوجد مخزن مسجل بالفعل باسم '{name}'.");
            }

            // If this is set as default, clear default from existing warehouses
            if (request.IsDefault)
            {
                var existingDefault = await _warehouseRepository.GetDefaultAsync(ct);
                if (existingDefault != null)
                {
                    existingDefault.IsDefault = false;
                    _warehouseRepository.Update(existingDefault);
                }
            }

            var warehouse = new Warehouse
            {
                Name = name,
                Location = request.Location?.Trim(),
                IsDefault = request.IsDefault,
                IsActive = request.IsActive
            };

            await _warehouseRepository.AddAsync(warehouse, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new WarehouseResponse
            {
                ID = warehouse.ID,
                Name = warehouse.Name,
                Location = warehouse.Location,
                IsDefault = warehouse.IsDefault,
                IsActive = warehouse.IsActive
            };
        }

        public async Task<WarehouseResponse> UpdateWarehouseAsync(int id, UpdateWarehouseRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var warehouse = await _warehouseRepository.GetByIdAsync(id, ct);
            if (warehouse == null)
            {
                throw new KeyNotFoundException($"المخزن برقم {id} غير موجود.");
            }

            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("اسم المخزن مطلوب.", nameof(request.Name));
            }

            if (await _warehouseRepository.ExistsByNameAsync(name, id, ct))
            {
                throw new InvalidOperationException($"يوجد مخزن مسجل بالفعل باسم '{name}'.");
            }

            if (request.IsDefault && !warehouse.IsDefault)
            {
                var existingDefault = await _warehouseRepository.GetDefaultAsync(ct);
                if (existingDefault != null && existingDefault.ID != id)
                {
                    existingDefault.IsDefault = false;
                    _warehouseRepository.Update(existingDefault);
                }
            }

            warehouse.Name = name;
            warehouse.Location = request.Location?.Trim();
            warehouse.IsDefault = request.IsDefault;
            warehouse.IsActive = request.IsActive;

            _warehouseRepository.Update(warehouse);
            await _unitOfWork.SaveChangesAsync(ct);

            return new WarehouseResponse
            {
                ID = warehouse.ID,
                Name = warehouse.Name,
                Location = warehouse.Location,
                IsDefault = warehouse.IsDefault,
                IsActive = warehouse.IsActive
            };
        }

        // Stock Balances & Movements
        public async Task<List<StockBalanceResponse>> GetBalancesAsync(int warehouseId, bool lowStockOnly = false, CancellationToken ct = default)
        {
            var balances = await _stockRepository.GetBalancesByWarehouseAsync(warehouseId, lowStockOnly, ct);
            return balances.Select(MapToBalanceResponse).ToList();
        }

        public async Task<StockBalanceResponse?> GetItemBalanceAsync(int warehouseId, int rmsItemId, CancellationToken ct = default)
        {
            var balance = await _stockRepository.GetBalanceAsync(warehouseId, rmsItemId, ct);
            return balance == null ? null : MapToBalanceResponse(balance);
        }

        public async Task<List<StockMovementResponse>> GetMovementsAsync(
            int? warehouseId = null,
            int? rmsItemId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var movements = await _stockRepository.GetMovementsAsync(warehouseId, rmsItemId, fromDate, toDate, ct);
            return movements.Select(m => new StockMovementResponse
            {
                ID = m.ID,
                WarehouseID = m.WarehouseID,
                WarehouseName = m.Warehouse?.Name ?? string.Empty,
                RMSItemID = m.RMSItemID,
                MovementType = m.MovementType,
                Quantity = m.Quantity,
                UnitCost = m.UnitCost,
                TotalCost = m.TotalCost,
                MovementDate = m.MovementDate,
                ReferenceType = m.ReferenceType,
                ReferenceID = m.ReferenceID,
                Notes = m.Notes,
                CreatedBy = m.CreatedBy
            }).ToList();
        }

        public async Task<StockBalanceResponse> AdjustStockAsync(StockAdjustmentRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.Quantity == 0)
            {
                throw new ArgumentException("كمية التسوية لا يمكن أن تكون صفر.", nameof(request.Quantity));
            }

            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseID, ct);
            if (warehouse == null)
            {
                throw new KeyNotFoundException($"المخزن برقم {request.WarehouseID} غير موجود.");
            }

            var balance = await _stockRepository.GetBalanceAsync(request.WarehouseID, request.RMSItemID, ct);

            if (balance == null)
            {
                if (request.Quantity < 0)
                {
                    throw new InvalidOperationException("لا يمكن تسوية صنف بالخصم ليس له رصيد مسجل بالمخزن.");
                }

                balance = new StockBalance
                {
                    WarehouseID = request.WarehouseID,
                    RMSItemID = request.RMSItemID,
                    QuantityOnHand = request.Quantity,
                    AverageCost = request.UnitCost >= 0 ? request.UnitCost : 0,
                    LastUpdatedAt = DateTime.Now
                };
                await _stockRepository.AddBalanceAsync(balance, ct);
            }
            else
            {
                var newQty = balance.QuantityOnHand + request.Quantity;
                if (newQty < 0)
                {
                    throw new InvalidOperationException($"الكمية الحالية بالمخزن ({balance.QuantityOnHand}) غير كافية لخصم ({Math.Abs(request.Quantity)}).");
                }

                if (request.Quantity > 0 && request.UnitCost > 0)
                {
                    // Weighted average cost update
                    var currentTotalValue = balance.QuantityOnHand * balance.AverageCost;
                    var addedValue = request.Quantity * request.UnitCost;
                    balance.AverageCost = Math.Round((currentTotalValue + addedValue) / newQty, 2);
                }

                balance.QuantityOnHand = newQty;
                balance.LastUpdatedAt = DateTime.Now;
                _stockRepository.UpdateBalance(balance);
            }

            var movement = new StockMovement
            {
                WarehouseID = request.WarehouseID,
                RMSItemID = request.RMSItemID,
                MovementType = request.Quantity > 0 ? StockMovementType.AdjustmentIn : StockMovementType.AdjustmentOut,
                Quantity = Math.Abs(request.Quantity),
                UnitCost = request.UnitCost,
                MovementDate = DateTime.Now,
                ReferenceType = "ManualAdjustment",
                Notes = request.Reason,
                CreatedBy = request.CreatedBy
            };

            await _stockRepository.AddMovementAsync(movement, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToBalanceResponse(balance);
        }

        private static StockBalanceResponse MapToBalanceResponse(StockBalance b)
        {
            return new StockBalanceResponse
            {
                ID = b.ID,
                WarehouseID = b.WarehouseID,
                WarehouseName = b.Warehouse?.Name ?? string.Empty,
                RMSItemID = b.RMSItemID,
                QuantityOnHand = b.QuantityOnHand,
                AverageCost = b.AverageCost,
                ReorderLevel = b.ReorderLevel,
                LastUpdatedAt = b.LastUpdatedAt,
                TotalValue = b.TotalValue,
                IsLowStock = b.IsLowStock
            };
        }
    }
}

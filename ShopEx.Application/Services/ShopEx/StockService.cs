using ShopEx.Application.DTOs.Stock;
using ShopEx.Application.interfaces.RMS.Repositories;
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
        private readonly IItemRepository _itemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StockService(
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IItemRepository itemRepository,
            IUnitOfWork unitOfWork)
        {
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
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
        private const int MaxPageSize = 100;
        private const int RecentMovementsCount = 30;

        public async Task<PagedStockBalanceResponse> GetBalancesAsync(
            int warehouseId, string? search = null, int page = 1, int pageSize = 20, bool includeTotals = true, CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // Needed for the warehouse value and for searching: one narrow row per in-stock item.
            List<(int RMSItemID, decimal Quantity)>? quantities = null;

            List<StockBalance> balances;
            int total;
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Item names live in RMS: search only among this warehouse's in-stock items, so the
                // result is never cut off by an RMS-wide match limit, then page the matching ids here.
                quantities = await _stockRepository.GetInStockQuantitiesAsync(warehouseId, ct);
                var matched = await _itemRepository.FilterIdsAsync(search, quantities.Select(q => q.RMSItemID), ct);
                matched.Sort();
                total = matched.Count;
                var pageIds = matched.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                balances = pageIds.Count == 0
                    ? new List<StockBalance>()
                    : (await _stockRepository.GetInStockBalancesPagedAsync(warehouseId, pageIds, 1, pageIds.Count, ct)).Items;
            }
            else
            {
                (balances, total) = await _stockRepository.GetInStockBalancesPagedAsync(warehouseId, null, page, pageSize, ct);
            }

            var rows = await MapWithItemInfoAsync(balances);
            var result = new PagedStockBalanceResponse
            {
                Items = rows, Page = page, PageSize = pageSize, TotalCount = total
            };

            if (includeTotals)
            {
                var (totalQty, totalCost) = await _stockRepository.GetInStockTotalsAsync(warehouseId, ct);
                // Prices live in RMS: value the whole warehouse (not just this page) at retail price.
                quantities ??= await _stockRepository.GetInStockQuantitiesAsync(warehouseId, ct);
                var prices = await _itemRepository.GetPricesByIdsAsync(quantities.Select(q => q.RMSItemID));
                result.TotalQuantity = totalQty;
                result.TotalCost = totalCost;
                result.TotalPrice = quantities.Sum(q => prices.TryGetValue(q.RMSItemID, out var p) ? q.Quantity * p : 0m);
            }

            return result;
        }

        public async Task<List<StockBalanceResponse>> GetAllInStockAsync(int warehouseId, CancellationToken ct = default)
        {
            var balances = await _stockRepository.GetInStockBalancesAsync(warehouseId, ct);
            return await MapWithItemInfoAsync(balances);
        }

        /// <summary>Maps balances and adds RMS item info with one batched (chunked) RMS lookup.</summary>
        private async Task<List<StockBalanceResponse>> MapWithItemInfoAsync(List<StockBalance> balances)
        {
            var items = await _itemRepository.GetByIdsAsync(balances.Select(b => b.RMSItemID));
            var rows = new List<StockBalanceResponse>(balances.Count);
            foreach (var balance in balances)
            {
                var row = MapToBalanceResponse(balance);
                if (items.TryGetValue(balance.RMSItemID, out var item))
                {
                    row.ItemDescription = item.Description ?? $"صنف #{item.ID}";
                    row.ItemLookupCode = item.ItemLookupCode;
                    row.RetailPrice = item.Price ?? 0;
                    row.Cost = item.Cost ?? balance.AverageCost;
                }
                else
                {
                    row.ItemDescription = $"صنف #{balance.RMSItemID}";
                    row.Cost = balance.AverageCost;
                }
                rows.Add(row);
            }
            return rows;
        }

        public async Task<StockBalanceResponse?> GetItemBalanceAsync(int warehouseId, int rmsItemId, CancellationToken ct = default)
        {
            var balance = await _stockRepository.GetBalanceAsync(warehouseId, rmsItemId, ct);
            if (balance == null) return null;

            var res = MapToBalanceResponse(balance);
            var item = await _itemRepository.GetByIdAsync(rmsItemId);
            if (item != null)
            {
                res.ItemDescription = item.Description ?? $"صنف #{item.ID}";
                res.ItemLookupCode = item.ItemLookupCode;
                res.RetailPrice = item.Price ?? 0;
                res.Cost = item.Cost ?? res.AverageCost;
            }
            return res;
        }

        public async Task<List<StockMovementResponse>> GetRecentMovementsAsync(int? warehouseId = null, CancellationToken ct = default)
        {
            var movements = await _stockRepository.GetRecentMovementsAsync(warehouseId, RecentMovementsCount, ct);
            var items = await _itemRepository.GetByIdsAsync(movements.Select(m => m.RMSItemID));

            return movements.Select(m => new StockMovementResponse
            {
                ID = m.ID,
                WarehouseID = m.WarehouseID,
                WarehouseName = m.Warehouse?.Name ?? string.Empty,
                RMSItemID = m.RMSItemID,
                ItemDescription = items.TryGetValue(m.RMSItemID, out var item) ? item.Description : null,
                IsIncoming = IncomingTypes.Contains(m.MovementType),
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

                // A new stock row is only allowed for a real RMS item
                if (await _itemRepository.GetByIdAsync(request.RMSItemID) == null)
                {
                    throw new ArgumentException($"الصنف رقم {request.RMSItemID} غير موجود في RMS.", nameof(request.RMSItemID));
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

        private static readonly HashSet<StockMovementType> IncomingTypes = new()
        {
            StockMovementType.OpeningBalance,
            StockMovementType.PurchaseIn,
            StockMovementType.CustomerReturnIn,
            StockMovementType.AdjustmentIn,
            StockMovementType.TransferIn,
            StockMovementType.ConsignmentReturnIn
        };

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

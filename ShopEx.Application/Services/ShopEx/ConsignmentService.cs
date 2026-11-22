using ShopEx.Application.DTOs.Consignment;
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
    public class ConsignmentService : IConsignmentService
    {
        private readonly IConsignmentRepository _consignmentRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConsignmentService(
            IConsignmentRepository consignmentRepository,
            IDealerRepository dealerRepository,
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IItemRepository itemRepository,
            IUnitOfWork unitOfWork)
        {
            _consignmentRepository = consignmentRepository ?? throw new ArgumentNullException(nameof(consignmentRepository));
            _dealerRepository = dealerRepository ?? throw new ArgumentNullException(nameof(dealerRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<ConsignmentResponse> CreateAsync(CreateConsignmentRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var dealer = await _dealerRepository.GetByIdAsync(request.DealerID, ct);
            if (dealer == null)
                throw new KeyNotFoundException($"التاجر برقم {request.DealerID} غير موجود.");

            if (!dealer.IsActive)
                throw new InvalidOperationException($"التاجر '{dealer.Name}' موقوف حالياً ولا يمكن فتح إذن أمانة له.");

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("يجب إضافة صنف واحد على الأقل في إذن الأمانة.", nameof(request.Items));

            foreach (var item in request.Items)
            {
                if (item.RMSItemID <= 0)
                    throw new ArgumentException("معرف الصنف غير صالح.");
                if (item.QuantityIssued <= 0)
                    throw new ArgumentException("كمية الصنف الصادرة يجب أن تكون أكبر من صفر.");
                if (item.UnitPrice < 0)
                    throw new ArgumentException("سعر الوحدة لا يمكن أن يكون سالباً.");
                if (item.UnitCost < 0)
                    throw new ArgumentException("تكلفة الوحدة لا يمكن أن تكون سالبة.");
            }

            if (request.AgreedDays.HasValue && request.AgreedDays.Value < 0)
                throw new ArgumentException("مدة الأمانة لا يمكن أن تكون سالبة.");

            // Resolve warehouse: the requested one, otherwise the default. Stock must always be deducted from a real warehouse.
            Warehouse? warehouse = null;
            if (request.WarehouseID.HasValue)
            {
                warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseID.Value, ct);
                if (warehouse == null)
                    throw new KeyNotFoundException($"المخزن برقم {request.WarehouseID.Value} غير موجود.");
            }
            warehouse ??= await _warehouseRepository.GetDefaultAsync(ct);
            if (warehouse == null)
                throw new InvalidOperationException("لا يوجد مخزن افتراضي لصرف البضاعة منه.");

            // Check availability (aggregated per item, in case the same item appears on more than one line)
            var balances = new Dictionary<int, StockBalance?>();
            foreach (var group in request.Items.GroupBy(i => i.RMSItemID))
            {
                var balance = await _stockRepository.GetBalanceAsync(warehouse.ID, group.Key, ct);
                balances[group.Key] = balance;

                var required = group.Sum(i => i.QuantityIssued);
                var available = balance?.QuantityOnHand ?? 0;
                if (available < required)
                {
                    var itemInfo = await _itemRepository.GetByIdAsync(group.Key);
                    var name = itemInfo?.Description ?? $"#{group.Key}";
                    throw new InvalidOperationException(
                        $"الرصيد المتاح من الصنف '{name}' بمخزن {warehouse.Name} ({available}) أقل من الكمية المطلوبة ({required}).");
                }
            }

            var agreedDays = request.AgreedDays.GetValueOrDefault(15);
            var issueDate = DateTime.Now;
            var dueDate = request.DueDate ?? issueDate.AddDays(agreedDays);
            if (dueDate.Date < issueDate.Date)
                throw new ArgumentException("تاريخ الاستحقاق لا يمكن أن يسبق تاريخ الصرف.");

            ConsignmentResponse? response = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var consignmentNumber = await _consignmentRepository.GetNextConsignmentNumberAsync(ct);

                var consignment = new Consignment
                {
                    ConsignmentNumber = consignmentNumber,
                    DealerID = dealer.ID,
                    Dealer = dealer,
                    WarehouseID = warehouse.ID,
                    IssueDate = issueDate,
                    AgreedDays = agreedDays,
                    DueDate = dueDate,
                    Status = ConsignmentStatus.Open,
                    Notes = request.Notes?.Trim(),
                    Items = request.Items.Select(i => new ConsignmentItem
                    {
                        RMSItemID = i.RMSItemID,
                        QuantityIssued = i.QuantityIssued,
                        UnitPrice = i.UnitPrice,
                        UnitCost = i.UnitCost
                    }).ToList()
                };

                await _consignmentRepository.AddAsync(consignment, ct);
                // Persist first so the stock movements reference the real consignment ID
                await _unitOfWork.SaveChangesAsync(ct);

                foreach (var item in consignment.Items)
                {
                    var balance = balances[item.RMSItemID]!; // existence guaranteed by the availability check above
                    balance.QuantityOnHand -= item.QuantityIssued;
                    balance.LastUpdatedAt = DateTime.Now;
                    _stockRepository.UpdateBalance(balance);

                    await _stockRepository.AddMovementAsync(new StockMovement
                    {
                        WarehouseID = warehouse.ID,
                        RMSItemID = item.RMSItemID,
                        MovementType = StockMovementType.ConsignmentOut,
                        Quantity = item.QuantityIssued,
                        UnitCost = item.UnitCost,
                        MovementDate = DateTime.Now,
                        ReferenceType = "Consignment",
                        ReferenceID = consignment.ID,
                        Notes = $"صرف بضاعة أمانة للتاجر: {dealer.Name} - إذن رقم {consignmentNumber}"
                    }, ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);
                response = await MapToResponseAsync(consignment, warehouse.Name);
            }, ct);

            return response!;
        }

        public async Task<ConsignmentResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _consignmentRepository.GetByIdAsync(id, ct);
            return entity == null ? null : await MapToResponseAsync(entity);
        }

        public async Task<ConsignmentResponse?> GetByNumberAsync(string consignmentNumber, CancellationToken ct = default)
        {
            var entity = await _consignmentRepository.GetByConsignmentNumberAsync(consignmentNumber, ct);
            return entity == null ? null : await MapToResponseAsync(entity);
        }

        public async Task<List<ConsignmentSummaryResponse>> GetAllAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            return await _consignmentRepository.GetSummariesAsync(dealerId, status, fromDate, toDate, ct);
        }

        public async Task<ConsignmentResponse> AddSettlementAsync(int id, CreateConsignmentSettlementRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var consignment = await _consignmentRepository.GetByIdAsync(id, ct);
            if (consignment == null)
                throw new KeyNotFoundException($"إذن الأمانة برقم {id} غير موجود.");

            if (consignment.Status != ConsignmentStatus.Open)
                throw new InvalidOperationException($"لا يمكن إضافة تسوية لإذن أمانة بحالة '{consignment.Status}'.");

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("يجب إدخال صنف واحد على الأقل في التسوية.", nameof(request.Items));

            // Merge duplicate lines of the same consignment item so the outstanding check is accurate
            var requested = request.Items
                .GroupBy(i => i.ConsignmentItemID)
                .Select(g => new { ConsignmentItemID = g.Key, Sold = g.Sum(x => x.QuantitySold), Returned = g.Sum(x => x.QuantityReturned) })
                .ToList();

            var settlementItems = new List<ConsignmentSettlementItem>();
            var returnLines = new List<(ConsignmentItem Item, decimal Qty)>();

            foreach (var req in requested)
            {
                var consignmentItem = consignment.Items.FirstOrDefault(i => i.ID == req.ConsignmentItemID);
                if (consignmentItem == null)
                    throw new ArgumentException($"الصنف ذو المعرف {req.ConsignmentItemID} لا ينتمي لإذن الأمانة هذا.");

                if (req.Sold < 0 || req.Returned < 0)
                    throw new ArgumentException("الكميات المباعة والمرتجعة لا يمكن أن تكون سالبة.");

                if (req.Sold == 0 && req.Returned == 0)
                    continue;

                var totalSettled = req.Sold + req.Returned;
                if (totalSettled > consignmentItem.QuantityOutstanding)
                {
                    throw new InvalidOperationException(
                        $"الكمية المطلوبة لتسوية الصنف {consignmentItem.RMSItemID} هي ({totalSettled})، بينما الكمية المتبقية بالأمانة هي ({consignmentItem.QuantityOutstanding}).");
                }

                settlementItems.Add(new ConsignmentSettlementItem
                {
                    ConsignmentItemID = consignmentItem.ID,
                    ConsignmentItem = consignmentItem,
                    QuantitySold = req.Sold,
                    QuantityReturned = req.Returned
                });

                if (req.Returned > 0)
                    returnLines.Add((consignmentItem, req.Returned));
            }

            if (!settlementItems.Any())
                throw new ArgumentException("يجب إدخال كميات مباعة أو مرتجعة في التسوية.");

            var settledSoldValue = settlementItems.Sum(i => i.QuantitySold * i.ConsignmentItem.UnitPrice);
            if (request.Payment != null && request.Payment.Amount < 0)
                throw new ArgumentException("مبلغ الدفعة لا يمكن أن يكون سالباً.");
            var hasPayment = request.Payment != null && request.Payment.Amount > 0;
            var dueAfterSettlement = consignment.RemainingAmount + settledSoldValue;
            if (hasPayment && request.Payment!.Amount > dueAfterSettlement)
            {
                throw new InvalidOperationException(
                    $"مبلغ التحصيل ({request.Payment.Amount:N2}) أكبر من المستحق على البنزينة ({dueAfterSettlement:N2}).");
            }

            // Returned goods go back to the warehouse they were issued from
            Warehouse? warehouse = null;
            if (returnLines.Count > 0)
            {
                warehouse = consignment.WarehouseID.HasValue
                    ? await _warehouseRepository.GetByIdAsync(consignment.WarehouseID.Value, ct)
                    : null;
                warehouse ??= await _warehouseRepository.GetDefaultAsync(ct);
                if (warehouse == null)
                    throw new InvalidOperationException("لا يوجد مخزن لاستقبال المرتجع.");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (item, qty) in returnLines)
                    await ReturnToStockAsync(warehouse!, item, qty, StockMovementType.ConsignmentReturnIn,
                        "ConsignmentSettlement", consignment.ID,
                        $"مرتجع بضاعة أمانة من التاجر: {consignment.Dealer?.Name} - إذن رقم {consignment.ConsignmentNumber}", ct);

                var settlement = new ConsignmentSettlement
                {
                    ConsignmentID = consignment.ID,
                    SettlementDate = DateTime.Now,
                    Notes = request.Notes?.Trim(),
                    CreatedBy = request.CreatedBy?.Trim(),
                    Items = settlementItems
                };

                await _consignmentRepository.AddSettlementAsync(settlement, ct);
                consignment.Settlements.Add(settlement);

                if (hasPayment)
                {
                    var payment = new ConsignmentPayment
                    {
                        ConsignmentID = consignment.ID,
                        Amount = request.Payment!.Amount,
                        PaymentDate = request.Payment.PaymentDate ?? DateTime.Now,
                        PaymentMethod = request.Payment.PaymentMethod?.Trim(),
                        Notes = request.Payment.Notes?.Trim()
                    };
                    await _consignmentRepository.AddPaymentAsync(payment, ct);
                    consignment.Payments.Add(payment);
                }

                CloseIfSettled(consignment);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await MapToResponseAsync(consignment);
        }

        public async Task<ConsignmentResponse> AddPaymentAsync(int id, AddConsignmentPaymentRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("مبلغ الدفعة يجب أن يكون أكبر من صفر.", nameof(request.Amount));

            var consignment = await _consignmentRepository.GetByIdAsync(id, ct);
            if (consignment == null)
                throw new KeyNotFoundException($"إذن الأمانة برقم {id} غير موجود.");

            if (consignment.Status == ConsignmentStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إضافة دفعة لإذن أمانة ملغي.");

            if (request.Amount > consignment.RemainingAmount)
            {
                throw new InvalidOperationException(
                    $"مبلغ الدفعة ({request.Amount:N2}) أكبر من المتبقي للتحصيل ({Math.Max(consignment.RemainingAmount, 0):N2}).");
            }

            var payment = new ConsignmentPayment
            {
                ConsignmentID = consignment.ID,
                Amount = request.Amount,
                PaymentDate = request.PaymentDate ?? DateTime.Now,
                PaymentMethod = request.PaymentMethod?.Trim(),
                Notes = request.Notes?.Trim()
            };

            await _consignmentRepository.AddPaymentAsync(payment, ct);
            consignment.Payments.Add(payment);

            CloseIfSettled(consignment);
            await _unitOfWork.SaveChangesAsync(ct);

            return await MapToResponseAsync(consignment);
        }

        public async Task<ConsignmentResponse> CancelConsignmentAsync(int id, CancellationToken ct = default)
        {
            var consignment = await _consignmentRepository.GetByIdAsync(id, ct);
            if (consignment == null)
                throw new KeyNotFoundException($"إذن الأمانة برقم {id} غير موجود.");

            if (consignment.Status != ConsignmentStatus.Open)
                throw new InvalidOperationException($"لا يمكن إلغاء إذن أمانة بحالة '{consignment.Status}'.");

            if (consignment.Settlements.Any())
                throw new InvalidOperationException("لا يمكن إلغاء إذن الأمانة لوجود تسويات تمت عليه بالفعل.");

            if (consignment.Payments.Any())
                throw new InvalidOperationException("لا يمكن إلغاء إذن الأمانة لوجود دفعات مسجلة عليه.");

            // Return stock back to the warehouse it was issued from
            Warehouse? warehouse = consignment.WarehouseID.HasValue
                ? await _warehouseRepository.GetByIdAsync(consignment.WarehouseID.Value, ct)
                : null;
            warehouse ??= await _warehouseRepository.GetDefaultAsync(ct);
            if (warehouse == null)
                throw new InvalidOperationException("لا يوجد مخزن لاستقبال البضاعة المسترجعة.");

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var item in consignment.Items)
                    await ReturnToStockAsync(warehouse, item, item.QuantityIssued, StockMovementType.ConsignmentReturnIn,
                        "ConsignmentCancel", consignment.ID,
                        $"إلغاء إذن أمانة {consignment.ConsignmentNumber} واسترجاع الكمية للمخزن", ct);

                consignment.Status = ConsignmentStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await MapToResponseAsync(consignment);
        }

        private async Task ReturnToStockAsync(
            Warehouse warehouse, ConsignmentItem item, decimal quantity, StockMovementType type,
            string referenceType, int referenceId, string notes, CancellationToken ct)
        {
            var balance = await _stockRepository.GetBalanceAsync(warehouse.ID, item.RMSItemID, ct);
            if (balance == null)
            {
                await _stockRepository.AddBalanceAsync(new StockBalance
                {
                    WarehouseID = warehouse.ID,
                    RMSItemID = item.RMSItemID,
                    QuantityOnHand = quantity,
                    AverageCost = item.UnitCost,
                    LastUpdatedAt = DateTime.Now
                }, ct);
            }
            else
            {
                balance.QuantityOnHand += quantity;
                balance.LastUpdatedAt = DateTime.Now;
                _stockRepository.UpdateBalance(balance);
            }

            await _stockRepository.AddMovementAsync(new StockMovement
            {
                WarehouseID = warehouse.ID,
                RMSItemID = item.RMSItemID,
                MovementType = type,
                Quantity = quantity,
                UnitCost = item.UnitCost,
                MovementDate = DateTime.Now,
                ReferenceType = referenceType,
                ReferenceID = referenceId,
                Notes = notes
            }, ct);
        }

        private static void CloseIfSettled(Consignment consignment)
        {
            if (consignment.Status == ConsignmentStatus.Open
                && consignment.IsFullyAccountedFor
                && consignment.RemainingAmount <= 0)
            {
                consignment.Status = ConsignmentStatus.Closed;
            }
        }

        private async Task<ConsignmentResponse> MapToResponseAsync(Consignment c, string? warehouseName = null)
        {
            var response = MapToResponse(c);

            if (warehouseName == null && c.WarehouseID.HasValue)
                warehouseName = (await _warehouseRepository.GetByIdAsync(c.WarehouseID.Value))?.Name;
            response.WarehouseID = c.WarehouseID;
            response.WarehouseName = warehouseName;

            var ids = response.Items.Select(i => i.RMSItemID).Distinct().ToList();
            if (ids.Count > 0)
            {
                var items = await _itemRepository.GetByIdsAsync(ids);
                foreach (var i in response.Items)
                {
                    if (items.TryGetValue(i.RMSItemID, out var rms))
                    {
                        i.ItemDescription = rms.Description;
                        i.ItemLookupCode = rms.ItemLookupCode;
                    }
                }
                foreach (var si in response.Settlements.SelectMany(x => x.Items))
                {
                    if (items.TryGetValue(si.RMSItemID, out var rms))
                        si.ItemDescription = rms.Description;
                }
            }

            return response;
        }

        private static ConsignmentResponse MapToResponse(Consignment c)
        {
            return new ConsignmentResponse
            {
                ID = c.ID,
                ConsignmentNumber = c.ConsignmentNumber,
                DealerID = c.DealerID,
                DealerName = c.Dealer?.Name ?? string.Empty,
                DealerPhone = c.Dealer?.Phone ?? string.Empty,
                IssueDate = c.IssueDate,
                AgreedDays = c.AgreedDays,
                DueDate = c.DueDate,
                Status = c.Status,
                Notes = c.Notes,

                TotalIssuedValue = c.TotalIssuedValue,
                TotalSoldValue = c.TotalSoldValue,
                PaidAmount = c.PaidAmount,
                RemainingAmount = c.RemainingAmount,
                OutstandingQuantity = c.OutstandingQuantity,
                OutstandingValue = c.OutstandingValue,

                IsFullyAccountedFor = c.IsFullyAccountedFor,
                IsOverdue = c.IsOverdue,

                Items = c.Items?.Select(i => new ConsignmentItemResponse
                {
                    ID = i.ID,
                    ConsignmentID = i.ConsignmentID,
                    RMSItemID = i.RMSItemID,
                    QuantityIssued = i.QuantityIssued,
                    UnitPrice = i.UnitPrice,
                    UnitCost = i.UnitCost,
                    QuantitySold = i.QuantitySold,
                    QuantityReturned = i.QuantityReturned,
                    QuantityOutstanding = i.QuantityOutstanding,
                    IssuedValue = i.IssuedValue,
                    SoldValue = i.SoldValue,
                    OutstandingValue = i.OutstandingValue,
                    Profit = i.Profit
                }).ToList() ?? new(),

                Settlements = c.Settlements?.Select(s => new ConsignmentSettlementResponse
                {
                    ID = s.ID,
                    ConsignmentID = s.ConsignmentID,
                    SettlementDate = s.SettlementDate,
                    Notes = s.Notes,
                    CreatedBy = s.CreatedBy,
                    AmountDue = s.AmountDue,
                    Items = s.Items?.Select(si => new ConsignmentSettlementItemResponse
                    {
                        ID = si.ID,
                        ConsignmentItemID = si.ConsignmentItemID,
                        RMSItemID = si.ConsignmentItem?.RMSItemID ?? 0,
                        QuantitySold = si.QuantitySold,
                        QuantityReturned = si.QuantityReturned,
                        SoldValue = si.SoldValue
                    }).ToList() ?? new()
                }).ToList() ?? new(),

                Payments = c.Payments?.Select(p => new ConsignmentPaymentResponse
                {
                    ID = p.ID,
                    ConsignmentID = p.ConsignmentID,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod,
                    Notes = p.Notes
                }).ToList() ?? new()
            };
        }
    }
}

using ShopEx.Application.DTOs.Consignment;
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
        private readonly IUnitOfWork _unitOfWork;

        public ConsignmentService(
            IConsignmentRepository consignmentRepository,
            IDealerRepository dealerRepository,
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IUnitOfWork unitOfWork)
        {
            _consignmentRepository = consignmentRepository ?? throw new ArgumentNullException(nameof(consignmentRepository));
            _dealerRepository = dealerRepository ?? throw new ArgumentNullException(nameof(dealerRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
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

            // Resolve Warehouse (defaults to HQ/default warehouse)
            Warehouse? warehouse = null;
            if (request.WarehouseID.HasValue)
            {
                warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseID.Value, ct);
            }
            if (warehouse == null)
            {
                warehouse = await _warehouseRepository.GetDefaultAsync(ct);
            }

            var agreedDays = request.AgreedDays.GetValueOrDefault(15);
            var issueDate = DateTime.Now;
            var dueDate = request.DueDate ?? issueDate.AddDays(agreedDays);
            var consignmentNumber = await _consignmentRepository.GetNextConsignmentNumberAsync(ct);

            var consignment = new Consignment
            {
                ConsignmentNumber = consignmentNumber,
                DealerID = dealer.ID,
                Dealer = dealer,
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

            // Deduct stock from warehouse if warehouse is available
            if (warehouse != null)
            {
                foreach (var item in consignment.Items)
                {
                    var balance = await _stockRepository.GetBalanceAsync(warehouse.ID, item.RMSItemID, ct);
                    if (balance == null)
                    {
                        balance = new StockBalance
                        {
                            WarehouseID = warehouse.ID,
                            RMSItemID = item.RMSItemID,
                            QuantityOnHand = -item.QuantityIssued,
                            AverageCost = item.UnitCost,
                            LastUpdatedAt = DateTime.Now
                        };
                        await _stockRepository.AddBalanceAsync(balance, ct);
                    }
                    else
                    {
                        balance.QuantityOnHand -= item.QuantityIssued;
                        balance.LastUpdatedAt = DateTime.Now;
                        _stockRepository.UpdateBalance(balance);
                    }

                    var movement = new StockMovement
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
                    };
                    await _stockRepository.AddMovementAsync(movement, ct);
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(consignment);
        }

        public async Task<ConsignmentResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _consignmentRepository.GetByIdAsync(id, ct);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<ConsignmentResponse?> GetByNumberAsync(string consignmentNumber, CancellationToken ct = default)
        {
            var entity = await _consignmentRepository.GetByConsignmentNumberAsync(consignmentNumber, ct);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<List<ConsignmentSummaryResponse>> GetAllAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var list = await _consignmentRepository.GetAllAsync(dealerId, status, fromDate, toDate, ct);
            return list.Select(MapToSummary).ToList();
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

            var settlementItems = new List<ConsignmentSettlementItem>();
            var warehouse = await _warehouseRepository.GetDefaultAsync(ct);

            foreach (var reqItem in request.Items)
            {
                var consignmentItem = consignment.Items.FirstOrDefault(i => i.ID == reqItem.ConsignmentItemID);
                if (consignmentItem == null)
                    throw new ArgumentException($"الصنف ذو المعرف {reqItem.ConsignmentItemID} لا ينتمي لإذن الأمانة هذا.");

                if (reqItem.QuantitySold < 0 || reqItem.QuantityReturned < 0)
                    throw new ArgumentException("الكميات المباعة والمرتجعة لا يمكن أن تكون سالبة.");

                if (reqItem.QuantitySold == 0 && reqItem.QuantityReturned == 0)
                    continue;

                var totalSettled = reqItem.QuantitySold + reqItem.QuantityReturned;
                if (totalSettled > consignmentItem.QuantityOutstanding)
                {
                    throw new InvalidOperationException(
                        $"الكمية المطلوبة لتسوية الصنف {consignmentItem.RMSItemID} هي ({totalSettled})، بينما الكمية المتبقية بالأمانة هي ({consignmentItem.QuantityOutstanding}).");
                }

                var settlementItem = new ConsignmentSettlementItem
                {
                    ConsignmentItemID = consignmentItem.ID,
                    ConsignmentItem = consignmentItem,
                    QuantitySold = reqItem.QuantitySold,
                    QuantityReturned = reqItem.QuantityReturned
                };
                settlementItems.Add(settlementItem);

                // If items are returned to warehouse, update stock
                if (reqItem.QuantityReturned > 0 && warehouse != null)
                {
                    var balance = await _stockRepository.GetBalanceAsync(warehouse.ID, consignmentItem.RMSItemID, ct);
                    if (balance == null)
                    {
                        balance = new StockBalance
                        {
                            WarehouseID = warehouse.ID,
                            RMSItemID = consignmentItem.RMSItemID,
                            QuantityOnHand = reqItem.QuantityReturned,
                            AverageCost = consignmentItem.UnitCost,
                            LastUpdatedAt = DateTime.Now
                        };
                        await _stockRepository.AddBalanceAsync(balance, ct);
                    }
                    else
                    {
                        balance.QuantityOnHand += reqItem.QuantityReturned;
                        balance.LastUpdatedAt = DateTime.Now;
                        _stockRepository.UpdateBalance(balance);
                    }

                    var movement = new StockMovement
                    {
                        WarehouseID = warehouse.ID,
                        RMSItemID = consignmentItem.RMSItemID,
                        MovementType = StockMovementType.ConsignmentReturnIn,
                        Quantity = reqItem.QuantityReturned,
                        UnitCost = consignmentItem.UnitCost,
                        MovementDate = DateTime.Now,
                        ReferenceType = "ConsignmentSettlement",
                        ReferenceID = consignment.ID,
                        Notes = $"مرتجع بضاعة أمانة من التاجر: {consignment.Dealer?.Name} - إذن رقم {consignment.ConsignmentNumber}"
                    };
                    await _stockRepository.AddMovementAsync(movement, ct);
                }
            }

            if (!settlementItems.Any())
                throw new ArgumentException("يجب إدخال كميات مباعة أو مرتجعة في التسوية.");

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

            // If a payment is provided alongside the settlement
            if (request.Payment != null && request.Payment.Amount > 0)
            {
                var payment = new ConsignmentPayment
                {
                    ConsignmentID = consignment.ID,
                    Amount = request.Payment.Amount,
                    PaymentDate = request.Payment.PaymentDate ?? DateTime.Now,
                    PaymentMethod = request.Payment.PaymentMethod?.Trim(),
                    Notes = request.Payment.Notes?.Trim()
                };
                await _consignmentRepository.AddPaymentAsync(payment, ct);
                consignment.Payments.Add(payment);
            }

            // Check if consignment is fully settled and paid
            if (consignment.IsFullyAccountedFor && consignment.RemainingAmount <= 0)
            {
                consignment.Status = ConsignmentStatus.Closed;
                _consignmentRepository.Update(consignment);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(consignment);
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

            // If fully accounted and fully paid, close
            if (consignment.IsFullyAccountedFor && consignment.RemainingAmount <= 0)
            {
                consignment.Status = ConsignmentStatus.Closed;
                _consignmentRepository.Update(consignment);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(consignment);
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

            // Return stock back to warehouse
            var warehouse = await _warehouseRepository.GetDefaultAsync(ct);
            if (warehouse != null)
            {
                foreach (var item in consignment.Items)
                {
                    var balance = await _stockRepository.GetBalanceAsync(warehouse.ID, item.RMSItemID, ct);
                    if (balance != null)
                    {
                        balance.QuantityOnHand += item.QuantityIssued;
                        balance.LastUpdatedAt = DateTime.Now;
                        _stockRepository.UpdateBalance(balance);
                    }

                    var movement = new StockMovement
                    {
                        WarehouseID = warehouse.ID,
                        RMSItemID = item.RMSItemID,
                        MovementType = StockMovementType.AdjustmentIn,
                        Quantity = item.QuantityIssued,
                        UnitCost = item.UnitCost,
                        MovementDate = DateTime.Now,
                        ReferenceType = "ConsignmentCancel",
                        ReferenceID = consignment.ID,
                        Notes = $"إلغاء إذن أمانة {consignment.ConsignmentNumber} واسترجاع الكمية للمخزن"
                    };
                    await _stockRepository.AddMovementAsync(movement, ct);
                }
            }

            consignment.Status = ConsignmentStatus.Cancelled;
            _consignmentRepository.Update(consignment);

            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(consignment);
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

        private static ConsignmentSummaryResponse MapToSummary(Consignment c)
        {
            return new ConsignmentSummaryResponse
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
                ItemsCount = c.Items?.Count ?? 0,
                TotalIssuedValue = c.TotalIssuedValue,
                TotalSoldValue = c.TotalSoldValue,
                PaidAmount = c.PaidAmount,
                RemainingAmount = c.RemainingAmount,
                OutstandingQuantity = c.OutstandingQuantity,
                OutstandingValue = c.OutstandingValue,
                IsFullyAccountedFor = c.IsFullyAccountedFor,
                IsOverdue = c.IsOverdue
            };
        }
    }
}

using ShopEx.Application.DTOs.SupplierReturn;
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
    public class SupplierReturnService : ISupplierReturnService
    {
        private readonly ISupplierReturnRepository _returnRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepository;
        private readonly ISupplierInvoiceRepository _invoiceRepository;

        public SupplierReturnService(
            ISupplierReturnRepository returnRepository,
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            ISupplierInvoiceRepository invoiceRepository)
        {
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _returnRepository = returnRepository ?? throw new ArgumentNullException(nameof(returnRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<SupplierReturnResponse> CreateAsync(CreateSupplierReturnRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.RMSSupplierID <= 0)
            {
                throw new ArgumentException("معرف المورد غير صالح.", nameof(request.RMSSupplierID));
            }

            if (await _supplierRepository.GetByIdAsync(request.RMSSupplierID) == null)
            {
                throw new KeyNotFoundException($"المورد برقم {request.RMSSupplierID} غير موجود.");
            }

            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseID, ct);
            if (warehouse == null)
            {
                throw new KeyNotFoundException($"المخزن برقم {request.WarehouseID} غير موجود.");
            }

            if (request.Items == null || !request.Items.Any())
            {
                throw new ArgumentException("يجب إضافة صنف واحد على الأقل في المرتجع.", nameof(request.Items));
            }

            foreach (var item in request.Items)
            {
                if (item.RMSItemID <= 0)
                    throw new ArgumentException("معرف الصنف غير صالح.");
                if (item.Quantity <= 0)
                    throw new ArgumentException("كمية المرتجع يجب أن تكون أكبر من صفر.");
                if (item.UnitPrice < 0)
                    throw new ArgumentException("سعر الوحدة لا يمكن أن يكون سالباً.");
            }

            // Merge duplicate item lines (same item + same price)
            var lines = request.Items
                .GroupBy(i => new { i.RMSItemID, UnitPrice = Math.Round(i.UnitPrice, 2, MidpointRounding.AwayFromZero), i.SupplierInvoiceItemID })
                .Select(g => new CreateSupplierReturnItemRequest
                {
                    RMSItemID = g.Key.RMSItemID,
                    UnitPrice = g.Key.UnitPrice,
                    SupplierInvoiceItemID = g.Key.SupplierInvoiceItemID,
                    Quantity = g.Sum(x => x.Quantity),
                    Reason = g.Select(x => x.Reason?.Trim()).FirstOrDefault(r => !string.IsNullOrEmpty(r))
                })
                .ToList();

            // Link to the original invoice: must belong to the same supplier, and
            // returned quantity can't exceed what was purchased minus earlier returns.
            if (request.SupplierInvoiceID.HasValue)
            {
                var invoice = await _invoiceRepository.GetByIdAsync(request.SupplierInvoiceID.Value, ct);
                if (invoice == null)
                    throw new KeyNotFoundException($"فاتورة المورد برقم {request.SupplierInvoiceID} غير موجودة.");
                if (invoice.RMSSupplierID != request.RMSSupplierID)
                    throw new InvalidOperationException("الفاتورة المحددة لا تخص هذا المورد.");

                var earlier = await _returnRepository.GetReturnedQuantitiesForInvoiceAsync(invoice.ID, ct);

                foreach (var line in lines)
                {
                    var invoiceItems = invoice.Items.Where(i => i.RMSItemID == line.RMSItemID).ToList();
                    if (invoiceItems.Count == 0)
                        throw new InvalidOperationException($"الصنف {line.RMSItemID} غير موجود في الفاتورة {invoice.InvoiceNumber}.");

                    var purchased = invoiceItems.Sum(i => i.Quantity);
                    var alreadyReturned = earlier.GetValueOrDefault(line.RMSItemID);
                    var requestedForItem = lines.Where(l => l.RMSItemID == line.RMSItemID).Sum(l => l.Quantity);
                    if (alreadyReturned + requestedForItem > purchased)
                        throw new InvalidOperationException(
                            $"كمية مرتجع الصنف {line.RMSItemID} ({requestedForItem}) تتجاوز المتاح للإرجاع من الفاتورة ({purchased - alreadyReturned}).");

                    line.SupplierInvoiceItemID ??= invoiceItems[0].ID;
                }
            }
            else if (lines.Any(l => l.SupplierInvoiceItemID.HasValue))
            {
                throw new InvalidOperationException("لا يمكن تحديد بند فاتورة بدون تحديد الفاتورة.");
            }

            var supplierReturn = new SupplierReturn
            {
                RMSSupplierID = request.RMSSupplierID,
                SupplierInvoiceID = request.SupplierInvoiceID,
                WarehouseID = request.WarehouseID,
                ReturnDate = request.ReturnDate ?? DateTime.Now,
                Status = SupplierReturnStatus.Draft,
                SettlementType = request.SettlementType,
                Notes = request.Notes?.Trim(),
                Items = lines.Select(i => new SupplierReturnItem
                {
                    SupplierInvoiceItemID = i.SupplierInvoiceItemID,
                    RMSItemID = i.RMSItemID,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Reason = i.Reason
                }).ToList()
            };

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Serialized numbering: two returns created at the same moment get consecutive numbers
                await _unitOfWork.AcquireLockAsync("SupplierReturn:Number", ct);
                supplierReturn.ReturnNumber = await _returnRepository.GetNextReturnNumberAsync(ct);
                await _returnRepository.AddAsync(supplierReturn, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                if (request.ConfirmImmediately)
                    await ApplyConfirmAsync(supplierReturn, ct);
            }, ct);

            return MapToResponse(supplierReturn);
        }

        public async Task<SupplierReturnResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var entity = await _returnRepository.GetByIdAsync(id, ct);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<SupplierReturnResponse?> GetByReturnNumberAsync(string returnNumber, CancellationToken ct = default)
        {
            var entity = await _returnRepository.GetByReturnNumberAsync(returnNumber, ct);
            return entity == null ? null : MapToResponse(entity);
        }

        public async Task<List<SupplierReturnSummaryResponse>> GetAllAsync(
            int? rmsSupplierId = null,
            SupplierReturnStatus? status = null,
            int? warehouseId = null,
            CancellationToken ct = default)
        {
            var list = await _returnRepository.GetAllAsync(rmsSupplierId, status, warehouseId, ct);
            return list.Select(r => new SupplierReturnSummaryResponse
            {
                ID = r.ID,
                ReturnNumber = r.ReturnNumber,
                RMSSupplierID = r.RMSSupplierID,
                SupplierInvoiceID = r.SupplierInvoiceID,
                WarehouseID = r.WarehouseID,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                ReturnDate = r.ReturnDate,
                Status = r.Status,
                SettlementType = r.SettlementType,
                ItemsCount = r.Items?.Count ?? 0,
                TotalAmount = r.TotalAmount,
                RefundedAmount = r.RefundedAmount,
                PendingRefund = r.PendingRefund
            }).ToList();
        }

        private static string ReturnLock(int returnId) => $"SupplierReturn:{returnId}";

        /// <summary>
        /// Runs <paramref name="work"/> in a transaction holding the return lock, on a freshly read return,
        /// so a double click (confirm twice, refund twice) runs one after the other and the second one fails its checks.
        /// </summary>
        private async Task<SupplierReturn> WithLockedReturnAsync(int returnId, Func<SupplierReturn, Task> work, CancellationToken ct)
        {
            SupplierReturn? entity = null;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.AcquireLockAsync(ReturnLock(returnId), ct);
                entity = await _returnRepository.GetByIdAsync(returnId, ct)
                         ?? throw new KeyNotFoundException($"مرتجع المورد برقم {returnId} غير موجود.");
                await work(entity);
            }, ct);
            return entity!;
        }

        public async Task<SupplierReturnResponse> ConfirmReturnAsync(int id, CancellationToken ct = default)
        {
            var entity = await WithLockedReturnAsync(id, async r =>
            {
                if (r.Status != SupplierReturnStatus.Draft)
                    throw new InvalidOperationException($"لا يمكن تأكيد المرتجع لأن حالته الحالية هي '{r.Status}'.");

                await ApplyConfirmAsync(r, ct);
            }, ct);

            return MapToResponse(entity);
        }

        public async Task<SupplierReturnResponse> CancelReturnAsync(int id, CancellationToken ct = default)
        {
            var entity = await WithLockedReturnAsync(id, async r =>
            {
                if (r.Status != SupplierReturnStatus.Draft)
                    throw new InvalidOperationException("لا يمكن إلغاء المرتجع إلا إذا كان في حالة مسودة (Draft).");

                r.Status = SupplierReturnStatus.Cancelled;
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return MapToResponse(entity);
        }

        public async Task<SupplierReturnResponse> AddRefundAsync(int returnId, AddSupplierRefundRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var refundAmount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            if (refundAmount <= 0)
            {
                throw new ArgumentException("مبلغ الاسترداد يجب أن يكون أكبر من صفر.", nameof(request.Amount));
            }

            var entity = await WithLockedReturnAsync(returnId, async r =>
            {
                // A draft is confirmed first (stock goes out) in the same transaction as the refund
                if (r.Status == SupplierReturnStatus.Draft)
                    await ApplyConfirmAsync(r, ct);
                else if (r.Status != SupplierReturnStatus.Confirmed)
                    throw new InvalidOperationException("لا يمكن تسجيل استرداد مالي لمرتجع ملغي.");

                if (refundAmount > Math.Round(r.PendingRefund, 2, MidpointRounding.AwayFromZero))
                {
                    throw new InvalidOperationException($"مبلغ الاسترداد ({refundAmount:N2}) يتجاوز المبلغ المتبقي المستحق ({r.PendingRefund:N2}).");
                }

                var refund = new SupplierRefund
                {
                    SupplierReturnID = returnId,
                    Amount = refundAmount,
                    RefundDate = request.RefundDate ?? DateTime.Now,
                    PaymentMethod = request.PaymentMethod?.Trim(),
                    Notes = request.Notes?.Trim()
                };

                await _returnRepository.AddRefundAsync(refund, ct);
                r.Refunds.Add(refund);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return MapToResponse(entity);
        }

        // Deducts stock, records movements, marks Confirmed and saves. Caller guarantees Draft status.
        private async Task ApplyConfirmAsync(SupplierReturn entity, CancellationToken ct)
        {
            var now = DateTime.Now;
            foreach (var item in entity.Items)
            {
                var balance = await _stockRepository.GetBalanceAsync(entity.WarehouseID, item.RMSItemID, ct);

                if (balance == null || balance.QuantityOnHand < item.Quantity)
                {
                    var available = balance?.QuantityOnHand ?? 0;
                    throw new InvalidOperationException(
                        $"الرصيد المتاح في المخزن للصنف {item.RMSItemID} هو ({available}) وهو غير كافٍ لمرتجع ({item.Quantity}).");
                }

                balance.QuantityOnHand -= item.Quantity;
                balance.LastUpdatedAt = now;
                _stockRepository.UpdateBalance(balance);

                await _stockRepository.AddMovementAsync(new StockMovement
                {
                    WarehouseID = entity.WarehouseID,
                    RMSItemID = item.RMSItemID,
                    MovementType = StockMovementType.SupplierReturnOut,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitPrice,
                    MovementDate = now,
                    ReferenceType = "SupplierReturn",
                    ReferenceID = entity.ID,
                    Notes = $"مرتجع مورد رقم {entity.ReturnNumber}",
                    CreatedBy = "System"
                }, ct);
            }

            entity.Status = SupplierReturnStatus.Confirmed;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static SupplierReturnResponse MapToResponse(SupplierReturn r)
        {
            return new SupplierReturnResponse
            {
                ID = r.ID,
                ReturnNumber = r.ReturnNumber,
                RMSSupplierID = r.RMSSupplierID,
                SupplierInvoiceID = r.SupplierInvoiceID,
                InvoiceNumber = r.SupplierInvoice?.InvoiceNumber,
                WarehouseID = r.WarehouseID,
                WarehouseName = r.Warehouse?.Name ?? string.Empty,
                ReturnDate = r.ReturnDate,
                Status = r.Status,
                SettlementType = r.SettlementType,
                Notes = r.Notes,
                TotalAmount = r.TotalAmount,
                RefundedAmount = r.RefundedAmount,
                PendingRefund = r.PendingRefund,
                Items = r.Items?.Select(i => new SupplierReturnItemResponse
                {
                    ID = i.ID,
                    SupplierReturnID = i.SupplierReturnID,
                    SupplierInvoiceItemID = i.SupplierInvoiceItemID,
                    RMSItemID = i.RMSItemID,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice,
                    Reason = i.Reason
                }).ToList() ?? new(),
                Refunds = r.Refunds?.Select(rf => new SupplierRefundResponse
                {
                    ID = rf.ID,
                    SupplierReturnID = rf.SupplierReturnID,
                    Amount = rf.Amount,
                    RefundDate = rf.RefundDate,
                    PaymentMethod = rf.PaymentMethod,
                    Notes = rf.Notes
                }).ToList() ?? new()
            };
        }
    }
}

using ShopEx.Application.DTOs.ShopEx;
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
    public class SupplierInvoiceService : ISupplierInvoiceService
    {
        private readonly ISupplierInvoiceRepository _invoiceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISupplierRepository _supplierRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IItemRepository _itemRepository;

        public SupplierInvoiceService(
            ISupplierInvoiceRepository invoiceRepository,
            IUnitOfWork unitOfWork,
            ISupplierRepository supplierRepository,
            IWarehouseRepository warehouseRepository,
            IStockRepository stockRepository,
            IItemRepository itemRepository)
        {
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
            _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        }

        public async Task<SupplierInvoiceResponse> CreateAsync(CreateSupplierInvoiceRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.InvoiceNumber))
                throw new ArgumentException("Invoice number is required.", nameof(request.InvoiceNumber));

            var invoiceNumber = request.InvoiceNumber.Trim();
            if (invoiceNumber.Length > 50)
                throw new ArgumentException("Invoice number cannot exceed 50 characters.", nameof(request.InvoiceNumber));

            var notes = request.Notes?.Trim();
            if (notes != null && notes.Length > 500)
                throw new ArgumentException("Notes cannot exceed 500 characters.", nameof(request.Notes));

            if (request.Items == null || !request.Items.Any())
                throw new ArgumentException("The invoice must contain at least one item.", nameof(request.Items));

            foreach (var item in request.Items)
            {
                if (item.RMSItemID <= 0)
                    throw new ArgumentException("Item id is invalid.", nameof(request.Items));
                if (item.Quantity <= 0)
                    throw new ArgumentException("Item quantity must be greater than zero.", nameof(request.Items));
                if (item.UnitPrice < 0)
                    throw new ArgumentException("Item unit price cannot be negative.", nameof(request.Items));
            }

            
            var supplier = await _supplierRepository.GetByIdAsync(request.RMSSupplierID);
            if (supplier == null)
                throw new KeyNotFoundException($"Supplier with ID {request.RMSSupplierID} was not found in RMS.");

            // Every line must be a real RMS item, otherwise stock would be created for an item that doesn't exist
            var itemIds = request.Items.Select(i => i.RMSItemID).Distinct().ToList();
            var knownItems = await _itemRepository.GetByIdsAsync(itemIds);
            var missing = itemIds.Where(id => !knownItems.ContainsKey(id)).ToList();
            if (missing.Count > 0)
                throw new ArgumentException(
                    $"الأصناف التالية غير موجودة في RMS: {string.Join(", ", missing)}.", nameof(request.Items));

           

            if (await _invoiceRepository.ExistsAsync(request.RMSSupplierID, invoiceNumber, ct))
                throw new InvalidOperationException($"Invoice number '{invoiceNumber}' already exists for supplier with ID {request.RMSSupplierID}.");

            
            var (subTotal, discountType, discountValue, discountAmount, totalAmount) = CalculateTotals(
                request.Items.Select(i => (i.Quantity, i.UnitPrice)), request.DiscountType, request.DiscountValue);

           
            var isCredit = request.PaymentType == SupplierPaymentType.Credit;
            var initialPayment = isCredit
                ? Math.Round(request.InitialPayment ?? 0m, 2, MidpointRounding.AwayFromZero)
                : totalAmount;

            if (initialPayment < 0 || initialPayment > totalAmount)
                throw new InvalidOperationException(
                    $"Initial payment ({initialPayment:N2}) must be between 0 and the invoice total ({totalAmount:N2}).");

            var remaining = totalAmount - initialPayment;
            var invoiceDate = request.InvoiceDate ?? DateTime.Now;

            
            int? creditDays = null;
            DateTime? dueDate = null;

            if (remaining > 0)
            {
                creditDays = request.CreditDays > 0 ? request.CreditDays : null;
                dueDate = request.DueDate
                          ?? (creditDays.HasValue ? invoiceDate.AddDays(creditDays.Value) : (DateTime?)null);

                if (dueDate == null)
                    throw new InvalidOperationException(
                        "Credit days or a due date is required when part of the invoice is unpaid.");

                if (dueDate.Value.Date < invoiceDate.Date)
                    throw new InvalidOperationException("Due date cannot be before the invoice date.");
            }

            
            var invoice = new SupplierInvoice
            {
                RMSSupplierID = request.RMSSupplierID,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = invoiceDate,
                SubTotal = subTotal,
                DiscountType = discountType,
                DiscountValue = discountValue,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Notes = notes,
                PaymentType = request.PaymentType,
                CreditDays = creditDays,
                DueDate = dueDate,
                Items = request.Items.Select(item => new SupplierInvoiceItem
                {
                    RMSItemID = item.RMSItemID,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            if (initialPayment > 0)
            {
                invoice.Payments.Add(new SupplierPayment
                {
                    Amount = initialPayment,
                    PaymentDate = invoiceDate,
                    PaymentMethod = request.InitialPaymentMethod?.Trim()
                });
            }

            RefreshStatus(invoice);

            // Determine target warehouse
            int targetWarehouseId;
            if (request.WarehouseID.HasValue && request.WarehouseID.Value > 0)
            {
                var targetWh = await _warehouseRepository.GetByIdAsync(request.WarehouseID.Value, ct);
                if (targetWh == null)
                    throw new KeyNotFoundException($"المخزن برقم {request.WarehouseID.Value} غير موجود.");
                targetWarehouseId = targetWh.ID;
            }
            else
            {
                var defaultWh = await _warehouseRepository.GetDefaultAsync(ct);
                if (defaultWh == null)
                    throw new InvalidOperationException("لم يتم العثور على مخزن افتراضي لإيداع الأصناف.");
                targetWarehouseId = defaultWh.ID;
            }

            // Spread the discount over the items so stock cost matches what is actually payable
            var costFactor = subTotal > 0 ? totalAmount / subTotal : 1m;

            // Merge duplicate item lines so each item is processed once against stock
            var stockLines = request.Items
                .GroupBy(i => i.RMSItemID)
                .Select(g => new
                {
                    RMSItemID = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    UnitPrice = g.Sum(x => x.Quantity * x.UnitPrice) / g.Sum(x => x.Quantity) * costFactor
                })
                .ToList();

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Save the invoice first so invoice.ID is real when stock movements reference it
                await _invoiceRepository.AddAsync(invoice, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                foreach (var line in stockLines)
                {
                    var unitCost = Math.Round(line.UnitPrice, 2, MidpointRounding.AwayFromZero);
                    var balance = await _stockRepository.GetBalanceAsync(targetWarehouseId, line.RMSItemID, ct);
                    if (balance == null)
                    {
                        balance = new StockBalance
                        {
                            WarehouseID = targetWarehouseId,
                            RMSItemID = line.RMSItemID,
                            QuantityOnHand = line.Quantity,
                            AverageCost = unitCost,
                            LastUpdatedAt = invoiceDate
                        };
                        await _stockRepository.AddBalanceAsync(balance, ct);
                    }
                    else
                    {
                        balance.AverageCost = WeightedAverageCost(balance.QuantityOnHand, balance.AverageCost, line.Quantity, line.UnitPrice);
                        balance.QuantityOnHand += line.Quantity;
                        balance.LastUpdatedAt = invoiceDate;
                        _stockRepository.UpdateBalance(balance);
                    }

                    await _stockRepository.AddMovementAsync(new StockMovement
                    {
                        WarehouseID = targetWarehouseId,
                        RMSItemID = line.RMSItemID,
                        MovementType = StockMovementType.PurchaseIn,
                        Quantity = line.Quantity,
                        UnitCost = unitCost,
                        MovementDate = invoiceDate,
                        ReferenceType = "SupplierInvoice",
                        ReferenceID = invoice.ID,
                        Notes = $"فاتورة مورد رقم {invoice.InvoiceNumber}",
                        CreatedBy = "System"
                    }, ct);
                }

                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);

            return await EnrichAsync(MapToResponse(invoice));
        }

        public async Task<SupplierInvoiceResponse> AddPaymentAsync(int invoiceId, AddPaymentRequest request, CancellationToken ct = default)
        {
            // Locked + re-read inside one transaction: two requests on the same record run one after the other
            SupplierInvoiceResponse? result = null;
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.AcquireLockAsync($"SupplierInvoice:{invoiceId}", ct);
                result = await AddPaymentCoreAsync(invoiceId, request, ct);
            }, ct);
            return result!;
        }

        private async Task<SupplierInvoiceResponse> AddPaymentCoreAsync(int invoiceId, AddPaymentRequest request, CancellationToken ct)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                throw new ArgumentException("Payment amount must be greater than zero.", nameof(request.Amount));

            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, ct);
            if (invoice == null)
                throw new KeyNotFoundException($"Supplier invoice with ID {invoiceId} was not found.");

            if (invoice.RemainingAmount <= 0)
                throw new InvalidOperationException($"Invoice '{invoice.InvoiceNumber}' is already fully paid.");

            if (amount > invoice.RemainingAmount)
                throw new InvalidOperationException(
                    $"Payment amount ({amount:N2}) exceeds remaining amount ({invoice.RemainingAmount:N2}).");

            invoice.Payments.Add(new SupplierPayment
            {
                SupplierInvoiceID = invoiceId,
                Amount = amount,
                PaymentDate = request.PaymentDate ?? DateTime.Now,
                PaymentMethod = request.PaymentMethod?.Trim()
            });

            RefreshStatus(invoice);

            await _unitOfWork.SaveChangesAsync(ct);

            return await EnrichAsync(MapToResponse(invoice));
        }

        public async Task<SupplierInvoiceResponse?> GetByIdAsync(int invoiceId, CancellationToken ct = default)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, ct);
            if (invoice == null)
                return null;

            return await EnrichAsync(MapToResponse(invoice));
        }

        public async Task<SupplierInvoicesPage> GetPagedAsync(
            int page, int pageSize, string? search, string? status, int? supplierId, CancellationToken ct = default)
        {
            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? Math.Min(pageSize, 100) : 10;

            // Supplier names live in the RMS database, so resolve the search term there first
            IReadOnlyCollection<int>? matchingSuppliers = null;
            if (!string.IsNullOrWhiteSpace(search))
                matchingSuppliers = await _supplierRepository.SearchIdsAsync(search);

            var result = await _invoiceRepository.GetPagedAsync(page, pageSize, search, matchingSuppliers, status, supplierId, ct);

            var suppliers = await _supplierRepository.GetByIdsAsync(result.Items.Select(i => i.RMSSupplierID));
            foreach (var row in result.Items)
                row.RMSSupplierName = suppliers.TryGetValue(row.RMSSupplierID, out var s) ? s.SupplierName : null;

            return result;
        }

        public async Task<List<SupplierInvoiceResponse>> GetAllAsync(CancellationToken ct = default)
        {
            var invoices = await _invoiceRepository.GetAllAsync(ct);
            return invoices.Select(MapToResponse).ToList();
        }

       
        // Adds supplier name + item descriptions (RMS lives in another database, so no join is possible)
        private async Task<SupplierInvoiceResponse> EnrichAsync(SupplierInvoiceResponse response)
        {
            var supplier = await _supplierRepository.GetByIdAsync(response.RMSSupplierID);
            response.RMSSupplierName = supplier?.SupplierName;

            var items = await _itemRepository.GetByIdsAsync(response.Items.Select(i => i.RMSItemID));
            foreach (var line in response.Items)
            {
                if (items.TryGetValue(line.RMSItemID, out var item))
                {
                    line.ItemDescription = item.Description;
                    line.ItemLookupCode = item.ItemLookupCode;
                }
            }
            return response;
        }

        /// <summary>
        /// Subtotal, invoice-level discount (percent or fixed amount, resolved to an amount) and net payable.
        /// Throws <see cref="ArgumentException"/> for a percent above 100 or a discount above the subtotal.
        /// </summary>
        public static (decimal SubTotal, SupplierDiscountType DiscountType, decimal DiscountValue, decimal DiscountAmount, decimal Total)
            CalculateTotals(IEnumerable<(decimal Quantity, decimal UnitPrice)> lines, SupplierDiscountType requestedType, decimal? requestedValue)
        {
            var subTotal = Math.Round(lines.Sum(l => l.Quantity * l.UnitPrice), 2, MidpointRounding.AwayFromZero);

            var discountValue = Math.Round(requestedValue ?? 0m, 2, MidpointRounding.AwayFromZero);
            var discountType = discountValue > 0 ? requestedType : SupplierDiscountType.None;
            decimal discountAmount = 0m;

            if (discountType == SupplierDiscountType.Percent)
            {
                if (discountValue > 100)
                    throw new ArgumentException("Discount percentage cannot exceed 100.", "DiscountValue");
                discountAmount = Math.Round(subTotal * discountValue / 100m, 2, MidpointRounding.AwayFromZero);
            }
            else if (discountType == SupplierDiscountType.Amount)
            {
                discountAmount = discountValue;
            }
            else
            {
                discountType = SupplierDiscountType.None;
                discountValue = 0m;
            }

            if (discountAmount > subTotal)
                throw new ArgumentException(
                    $"Discount ({discountAmount:N2}) cannot exceed the invoice subtotal ({subTotal:N2}).", "DiscountValue");

            return (subTotal, discountType, discountValue, discountAmount, subTotal - discountAmount);
        }

        /// <summary>
        /// Moving average cost after receiving <paramref name="addedQty"/> at <paramref name="addedUnitCost"/>.
        /// Negative stock (sold before it was received) carries no value, so it is not part of the average:
        /// otherwise the cost would be divided by a smaller net quantity and inflated.
        /// </summary>
        public static decimal WeightedAverageCost(decimal currentQty, decimal currentAvgCost, decimal addedQty, decimal addedUnitCost)
        {
            var valuedQty = Math.Max(currentQty, 0m);
            var totalQty = valuedQty + addedQty;
            if (totalQty <= 0)
                return currentAvgCost;
            return Math.Round((valuedQty * currentAvgCost + addedQty * addedUnitCost) / totalQty, 2);
        }

        private static void RefreshStatus(SupplierInvoice invoice)
        {
            var paid = invoice.PaidAmount;

            invoice.SupplierInvoiceStatus =
                paid >= invoice.TotalAmount ? SupplierInvoiceStatus.Paid
                : paid > 0 ? SupplierInvoiceStatus.PartiallyPaid
                                            : SupplierInvoiceStatus.Unpaid;
        }

        private static SupplierInvoiceResponse MapToResponse(SupplierInvoice invoice)
        {
            return new SupplierInvoiceResponse
            {
                ID = invoice.ID,
                RMSSupplierID = invoice.RMSSupplierID,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                SubTotal = invoice.SubTotal,
                DiscountType = invoice.DiscountType,
                DiscountValue = invoice.DiscountValue,
                DiscountAmount = invoice.DiscountAmount,
                TotalAmount = invoice.TotalAmount,
                Notes = invoice.Notes,
                SupplierInvoiceStatus = invoice.SupplierInvoiceStatus,
                PaymentType = invoice.PaymentType,
                CreditDays = invoice.CreditDays,
                DueDate = invoice.DueDate,
                PaidAmount = invoice.PaidAmount,
                RemainingAmount = invoice.RemainingAmount,
                Items = invoice.Items?.Select(i => new SupplierInvoiceItemResponse
                {
                    ID = i.ID,
                    SupplierInvoiceID = i.SupplierInvoiceID,
                    RMSItemID = i.RMSItemID,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList() ?? new List<SupplierInvoiceItemResponse>(),
                Payments = invoice.Payments?.Select(p => new SupplierPaymentResponse
                {
                    ID = p.ID,
                    SupplierInvoiceID = p.SupplierInvoiceID,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod
                }).ToList() ?? new List<SupplierPaymentResponse>()
            };
        }
    }
}
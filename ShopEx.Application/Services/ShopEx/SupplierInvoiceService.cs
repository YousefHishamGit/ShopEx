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
                if (item.Quantity <= 0)
                    throw new ArgumentException("Item quantity must be greater than zero.", nameof(request.Items));
                if (item.UnitPrice < 0)
                    throw new ArgumentException("Item unit price cannot be negative.", nameof(request.Items));
            }

            
            var supplier = await _supplierRepository.GetByIdAsync(request.RMSSupplierID);
            if (supplier == null)
                throw new KeyNotFoundException($"Supplier with ID {request.RMSSupplierID} was not found in RMS.");

           

            if (await _invoiceRepository.ExistsAsync(request.RMSSupplierID, invoiceNumber, ct))
                throw new InvalidOperationException($"Invoice number '{invoiceNumber}' already exists for supplier with ID {request.RMSSupplierID}.");

            
            var totalAmount = Math.Round(
                request.Items.Sum(i => i.Quantity * i.UnitPrice), 2, MidpointRounding.AwayFromZero);

           
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

            // Merge duplicate item lines so each item is processed once against stock
            var stockLines = request.Items
                .GroupBy(i => i.RMSItemID)
                .Select(g => new
                {
                    RMSItemID = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    UnitPrice = g.Sum(x => x.Quantity * x.UnitPrice) / g.Sum(x => x.Quantity)
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
                        var newQty = balance.QuantityOnHand + line.Quantity;
                        if (newQty > 0)
                        {
                            var currentTotalValue = Math.Max(balance.QuantityOnHand, 0) * balance.AverageCost;
                            var addedValue = line.Quantity * line.UnitPrice;
                            balance.AverageCost = Math.Round((currentTotalValue + addedValue) / newQty, 2);
                        }
                        balance.QuantityOnHand = newQty;
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
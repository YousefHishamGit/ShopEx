using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.DTOs.ShopEx;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.RMS.Service;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Enum;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.RMS
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IItemRepository _itemRepository;

        public SupplierService(
            ISupplierRepository supplierRepository,
            IItemRepository itemRepository)
        {
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        }

        public async Task<IEnumerable<SupplierResponse>> GetAllSuppliersAsync()
        {
            var Items = await _supplierRepository.GetAllAsync();
            if (Items == null)
            {
                return null;
            }
            var supplierResponses = Items.Select(s => new SupplierResponse
            {
              ID = s.ID,
              SupplierName=s.SupplierName,
              Code=s.Code,
                Phone=s.PhoneNumber

            });
            return supplierResponses;
        }

        public async Task<IEnumerable<ItemResponse>> GetItemsBySupplierAsync(int supplierId)
        {
       
            var supplier = await _supplierRepository.GetByIdAsync(supplierId);
            if (supplier == null)
            {
                return null; 
            }
            var items = await _itemRepository.GetItemsBySupplierIdAsync(supplierId);
            if (items == null)
            {
                return null;
            }
            var itemResponses = items.Select(i => new ItemResponse
            {
                ID = i.ID,
                Description = i.Description,
                SupplierID = i.SupplierID,
                Price = i.Price,
                PriceA = i.PriceA,
                Cost = i.Cost,
                LastCost = i.LastCost,
                ItemLookupCode = i.ItemLookupCode,
                Quantity = i.Quantity
            });

            return  itemResponses;
        }

        public async Task<(List<SupplierResponse> Items, int TotalCount)> GetSuppliersPagedAsync(string? search, int page, int pageSize)
        {
            var (suppliers, total) = await _supplierRepository.GetPagedAsync(search, page, pageSize);
            var items = suppliers.Select(s => new SupplierResponse
            {
                ID = s.ID,
                SupplierName = s.SupplierName,
                Code = s.Code,
                Phone = s.PhoneNumber
            }).ToList();
            return (items, total);
        }

        public async Task<SupplierStatementDto?> GetSupplierStatementAsync(
            int supplierId,
            string? status = null,
            string? section = null,
            int page = 1,
            int pageSize = 10,
            string? search = null)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId);
            if (supplier == null)
                return null;

            int pSize = pageSize > 0 ? Math.Min(pageSize, 100) : 10;
            int pNum = page > 0 ? page : 1;

            // Totals + counts come from SQL aggregates (no rows loaded)
            var summary = await _supplierRepository.GetAccountSummaryAsync(supplierId);

            // Balance owed to supplier = unpaid invoices - (confirmed returns - cash already refunded)
            var totalRemaining = (summary.TotalInvoices - summary.TotalPaid)
                                 - (summary.TotalReturns - summary.TotalRefunds);

            var dto = new SupplierStatementDto
            {
                SupplierId = supplier.ID,
                SupplierName = supplier.SupplierName,
                Code = supplier.Code,
                AccountNumber = supplier.AccountNumber,
                PhoneNumber = supplier.PhoneNumber,
                EmailAddress = supplier.EmailAddress,
                TotalInvoicesAmount = summary.TotalInvoices,
                TotalPaidAmount = summary.TotalPaid,
                TotalReturnsAmount = summary.TotalReturns,
                TotalRefundsAmount = summary.TotalRefunds,
                TotalPendingRefundsAmount = summary.TotalPendingRefunds,
                TotalRemainingAmount = totalRemaining,
                UnpaidCount = summary.UnpaidCount,
                PartialCount = summary.PartialCount,
                PaidCount = summary.PaidCount,
                TotalInvoicesCount = summary.InvoicesCount,
                ReturnsCount = summary.ReturnsCount,
                InvoicesPageNumber = pNum,
                InvoicesPageSize = pSize,
                ReturnsPageNumber = pNum,
                ReturnsPageSize = pSize
            };

            // "summary" (or empty) section => header + totals only, no rows
            if (string.Equals(section, "returns", StringComparison.OrdinalIgnoreCase))
            {
                var (returns, total) = await _supplierRepository.GetReturnsPageAsync(supplierId, search, pNum, pSize);
                dto.ReturnsTotalCount = total;
                dto.ReturnsTotalPages = Math.Max(1, (int)Math.Ceiling((double)total / pSize));
                dto.Returns = returns.Select(ret => new SupplierReturnStatementDto
                {
                    ID = ret.ID,
                    ReturnNumber = ret.ReturnNumber,
                    ReturnDate = ret.ReturnDate,
                    WarehouseID = ret.WarehouseID,
                    WarehouseName = ret.Warehouse?.Name ?? string.Empty,
                    TotalAmount = ret.TotalAmount,
                    RefundedAmount = ret.RefundedAmount,
                    PendingRefund = ret.PendingRefund,
                    Status = ret.Status.ToString(),
                    SettlementType = ret.SettlementType.ToString(),
                    ItemsCount = ret.Items?.Count ?? 0,
                    Notes = ret.Notes
                }).ToList();
            }
            else if (string.Equals(section, "invoices", StringComparison.OrdinalIgnoreCase))
            {
                var bucket = (status ?? "unpaid").ToLowerInvariant();
                if (bucket != "paid" && bucket != "partial" && bucket != "all") bucket = "unpaid";

                var (invoices, total) = await _supplierRepository.GetInvoicesPageAsync(supplierId, bucket, search, pNum, pSize);
                dto.InvoicesTotalCount = total;
                dto.InvoicesTotalPages = Math.Max(1, (int)Math.Ceiling((double)total / pSize));

                // Show readable item names in the expanded invoice row
                var itemIds = invoices.SelectMany(i => i.Items).Select(i => i.RMSItemID);
                var itemNames = await _itemRepository.GetByIdsAsync(itemIds);

                dto.Invoices = invoices.Select(inv => new SupplierInvoiceStatementDto
                {
                    ID = inv.ID,
                    InvoiceNumber = inv.InvoiceNumber,
                    InvoiceDate = inv.InvoiceDate,
                    TotalAmount = inv.TotalAmount,
                    PaidAmount = inv.PaidAmount,
                    RemainingAmount = inv.RemainingAmount,
                    Status = inv.SupplierInvoiceStatus.ToString(),
                    PaymentType = inv.PaymentType.ToString(),
                    DueDate = inv.DueDate,
                    Items = inv.Items.Select(item => new SupplierInvoiceItemDto
                    {
                        ID = item.ID,
                        RMSItemID = item.RMSItemID,
                        ItemDescription = itemNames.TryGetValue(item.RMSItemID, out var it) ? it.Description : null,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice
                    }).ToList(),
                    Payments = inv.Payments.OrderByDescending(p => p.PaymentDate).Select(pay => new SupplierPaymentDto
                    {
                        ID = pay.ID,
                        Amount = pay.Amount,
                        PaymentDate = pay.PaymentDate,
                        PaymentMethod = pay.PaymentMethod
                    }).ToList()
                }).ToList();
            }

            return dto;
        }
    }
}

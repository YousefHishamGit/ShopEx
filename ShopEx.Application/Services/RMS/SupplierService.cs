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
        private readonly ISupplierReturnRepository _returnRepository;

        public SupplierService(
            ISupplierRepository supplierRepository,
            IItemRepository itemRepository,
            ISupplierReturnRepository returnRepository)
        {
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
            _returnRepository = returnRepository ?? throw new ArgumentNullException(nameof(returnRepository));
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
              Code=s.Code
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
                PriceA= i.PriceA,
                Cost = i.Cost,

            });

            return  itemResponses;
        }

        public async Task<SupplierStatementDto?> GetSupplierStatementAsync(int supplierId)
        {
            var supplier = await _supplierRepository.GetByIdAsync(supplierId);
            if (supplier == null)
                return null;

            var invoices = await _supplierRepository.GetInvoicesBySupplierIdAsync(supplierId);
            var returns = await _returnRepository.GetAllAsync(rmsSupplierId: supplierId);

            var invoiceDtos = invoices.Select(inv => new SupplierInvoiceStatementDto
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
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList(),
                Payments = inv.Payments.Select(pay => new SupplierPaymentDto
                {
                    ID = pay.ID,
                    Amount = pay.Amount,
                    PaymentDate = pay.PaymentDate,
                    PaymentMethod = pay.PaymentMethod
                }).ToList()
            }).ToList();

            var returnDtos = returns.Select(ret => new SupplierReturnStatementDto
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

            var totalInvoices = invoiceDtos.Sum(i => i.TotalAmount);
            var totalPaid = invoiceDtos.Sum(i => i.PaidAmount);

            // Confirmed returns impact the supplier balance
            var confirmedReturns = returns.Where(r => r.Status == SupplierReturnStatus.Confirmed).ToList();
            var totalReturns = confirmedReturns.Sum(r => r.TotalAmount);
            var totalRefunds = confirmedReturns.Sum(r => r.RefundedAmount);

            // Net reduction on remaining debt to supplier:
            // For CreditToAccount: decreases debt by TotalAmount
            // For CashRefund: decreases debt by (TotalAmount - RefundedAmount) until cash is received
            var returnDeduction = confirmedReturns.Sum(r =>
                r.SettlementType == SupplierReturnSettlementType.CashRefund
                    ? (r.TotalAmount - r.RefundedAmount)
                    : r.TotalAmount
            );

            var totalRemaining = (totalInvoices - totalPaid) - returnDeduction;

            return new SupplierStatementDto
            {
                SupplierId = supplier.ID,
                SupplierName = supplier.SupplierName,
                Code = supplier.Code,
                AccountNumber = supplier.AccountNumber,
                PhoneNumber = supplier.PhoneNumber,
                EmailAddress = supplier.EmailAddress,
                TotalInvoicesAmount = totalInvoices,
                TotalPaidAmount = totalPaid,
                TotalReturnsAmount = totalReturns,
                TotalRefundsAmount = totalRefunds,
                TotalRemainingAmount = totalRemaining,
                Invoices = invoiceDtos,
                Returns = returnDtos
            };
        }
    }
}

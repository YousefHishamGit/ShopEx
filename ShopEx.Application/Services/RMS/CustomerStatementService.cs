using ShopEx.Application.DTOs.RMS;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.RMS.Service;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.RMS
{
    public class CustomerStatementService : ICustomerStatementService
    {
        private readonly ICustomerOrderRepository _orderRepository;

        public CustomerStatementService(ICustomerOrderRepository orderRepository)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        }

        public async Task<PagedResponse<CustomerSummaryDto>> GetPagedCustomersAsync(
            string? search = null,
            int? minDaysOld = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var (customers, totalCount, stats) = await _orderRepository.GetPagedCustomersAsync(
                search, minDaysOld, page, pageSize, ct);

            var items = customers.Select(c =>
            {
                stats.TryGetValue(c.ID, out var s);
                return new CustomerSummaryDto
                {
                    ID = c.ID,
                    AccountNumber = c.AccountNumber ?? string.Empty,
                    CustomerName = FormatCustomerName(c),
                    PhoneNumber = c.PhoneNumber ?? string.Empty,
                    Address = c.Address,
                    AccountBalance = c.AccountBalance ?? 0,
                    OpenInvoicesCount = s.OpenCount,
                    TotalOpenInvoicesAmount = s.OpenAmount,
                    OldestOpenInvoiceDays = s.OpenCount > 0 ? s.MaxDaysOld : null
                };
            }).ToList();

            return new PagedResponse<CustomerSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResponse<CustomerOpenInvoiceDto>> GetPagedOpenInvoicesAsync(
            int? customerId = null,
            int? storeId = null,
            int? minDaysOld = null,
            string? search = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            var (orders, totalCount) = await _orderRepository.GetPagedOpenOrdersAsync(
                customerId, storeId, minDaysOld, search, page, pageSize, ct);

            var storeNames = await _orderRepository.GetStoreNamesAsync(ct);
            var custIds = orders.Where(o => o.CustomerID.HasValue).Select(o => o.CustomerID!.Value);
            var customerInfo = await _orderRepository.GetCustomerInfoAsync(custIds, ct);

            var today = DateTime.Today;

            var items = orders.Select(o =>
            {
                var total = o.Total ?? 0;
                var deposit = o.Deposit ?? 0;
                var remaining = total - deposit;

                var storeName = (o.StoreID.HasValue && storeNames.TryGetValue(o.StoreID.Value, out var sName))
                    ? sName
                    : (o.StoreID.HasValue ? $"فرع #{o.StoreID.Value}" : "الفرع الرئيسي");

                string custName = "عميل غير مسجل";
                string accNumber = string.Empty;
                string phone = string.Empty;

                if (o.CustomerID.HasValue && customerInfo.TryGetValue(o.CustomerID.Value, out var cInfo))
                {
                    custName = cInfo.Name;
                    accNumber = cInfo.AccountNumber;
                    phone = cInfo.Phone;
                }

                var daysOld = o.Time.HasValue ? (today - o.Time.Value.Date).Days : 0;

                return new CustomerOpenInvoiceDto
                {
                    InvoiceID = o.ID,
                    InvoiceDate = o.Time,
                    DaysOld = daysOld,
                    CustomerID = o.CustomerID ?? 0,
                    CustomerName = custName,
                    AccountNumber = accNumber,
                    PhoneNumber = phone,
                    StoreID = o.StoreID,
                    StoreName = storeName,
                    TotalAmount = total,
                    Deposit = deposit,
                    RemainingAmount = remaining,
                    Comment = o.Comment,
                    ReferenceNumber = o.ReferenceNumber,
                    ItemsCount = 0
                };
            }).ToList();

            return new PagedResponse<CustomerOpenInvoiceDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<CustomerStatementDto?> GetCustomerStatementAsync(
            int customerId,
            int? minDaysOld = null,
            CancellationToken ct = default)
        {
            var customer = await _orderRepository.GetCustomerByIdAsync(customerId, ct);
            if (customer == null) return null;

            var openOrders = await _orderRepository.GetOpenOrdersByCustomerIdAsync(customerId, minDaysOld, ct);
            var storeNames = await _orderRepository.GetStoreNamesAsync(ct);

            var today = DateTime.Today;
            var custName = FormatCustomerName(customer);

            var invoices = openOrders.Select(o =>
            {
                var total = o.Total ?? 0;
                var deposit = o.Deposit ?? 0;
                var remaining = total - deposit;
                var storeName = (o.StoreID.HasValue && storeNames.TryGetValue(o.StoreID.Value, out var sName))
                    ? sName
                    : (o.StoreID.HasValue ? $"فرع #{o.StoreID.Value}" : "الفرع الرئيسي");

                var daysOld = o.Time.HasValue ? (today - o.Time.Value.Date).Days : 0;

                return new CustomerOpenInvoiceDto
                {
                    InvoiceID = o.ID,
                    InvoiceDate = o.Time,
                    DaysOld = daysOld,
                    CustomerID = customer.ID,
                    CustomerName = custName,
                    AccountNumber = customer.AccountNumber ?? string.Empty,
                    PhoneNumber = customer.PhoneNumber ?? string.Empty,
                    StoreID = o.StoreID,
                    StoreName = storeName,
                    TotalAmount = total,
                    Deposit = deposit,
                    RemainingAmount = remaining,
                    Comment = o.Comment,
                    ReferenceNumber = o.ReferenceNumber,
                    ItemsCount = 0
                };
            }).ToList();

            return new CustomerStatementDto
            {
                CustomerID = customer.ID,
                AccountNumber = customer.AccountNumber ?? string.Empty,
                CustomerName = custName,
                PhoneNumber = customer.PhoneNumber ?? string.Empty,
                Address = customer.Address,
                AccountBalance = customer.AccountBalance ?? 0,
                MinDaysOldFilter = minDaysOld,
                TotalOpenInvoicesCount = invoices.Count,
                TotalOpenInvoicesAmount = invoices.Sum(i => i.TotalAmount),
                TotalOpenInvoicesRemaining = invoices.Sum(i => i.RemainingAmount),
                OpenInvoices = invoices
            };
        }

        public async Task<CustomerStatementDto?> GetCustomerStatementByAccountAsync(
            string accountNumber,
            int? minDaysOld = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(accountNumber)) return null;

            var customer = await _orderRepository.GetCustomerByAccountNumberAsync(accountNumber.Trim(), ct);
            if (customer == null) return null;

            return await GetCustomerStatementAsync(customer.ID, minDaysOld, ct);
        }

        public async Task<CustomerInvoiceDetailDto?> GetInvoiceDetailAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId, ct);
            if (order == null) return null;

            Customer? customer = null;
            if (order.CustomerID.HasValue)
            {
                customer = await _orderRepository.GetCustomerByIdAsync(order.CustomerID.Value, ct);
            }

            Store? store = null;
            if (order.StoreID.HasValue)
            {
                store = await _orderRepository.GetStoreByIdAsync(order.StoreID.Value, ct);
            }

            var entries = await _orderRepository.GetOrderEntriesAsync(orderId, ct);
            var itemIds = entries.Where(e => e.ItemID.HasValue).Select(e => e.ItemID!.Value);
            var itemInfo = await _orderRepository.GetItemInfoAsync(itemIds, ct);

            var items = entries.Select(e =>
            {
                string productName = string.Empty;
                string? lookupCode = null;

                if (e.ItemID.HasValue && itemInfo.TryGetValue(e.ItemID.Value, out var info))
                {
                    productName = !string.IsNullOrWhiteSpace(info.Description) ? info.Description : (e.Description ?? string.Empty);
                    lookupCode = info.LookupCode;
                }
                else
                {
                    productName = e.Description ?? "صنف غير معرف";
                }

                var qty = e.QuantityOnOrder ?? 0;
                var unitPrice = e.Price ?? 0;
                var totalPrice = (decimal)qty * unitPrice;

                return new CustomerInvoiceItemDto
                {
                    EntryID = e.ID,
                    ItemID = e.ItemID,
                    ItemLookupCode = lookupCode,
                    ProductName = productName,
                    Quantity = qty,
                    UnitPrice = unitPrice,
                    FullPrice = e.FullPrice,
                    TotalPrice = totalPrice
                };
            }).ToList();

            var total = order.Total ?? 0;
            var deposit = order.Deposit ?? 0;
            var daysOld = order.Time.HasValue ? (DateTime.Today - order.Time.Value.Date).Days : 0;

            return new CustomerInvoiceDetailDto
            {
                InvoiceID = order.ID,
                InvoiceDate = order.Time,
                DaysOld = daysOld,
                CustomerID = customer?.ID ?? order.CustomerID ?? 0,
                AccountNumber = customer?.AccountNumber ?? string.Empty,
                CustomerName = customer != null ? FormatCustomerName(customer) : "عميل غير مسجل",
                PhoneNumber = customer?.PhoneNumber ?? string.Empty,
                StoreID = order.StoreID,
                StoreName = store?.Name ?? (order.StoreID.HasValue ? $"فرع #{order.StoreID.Value}" : "الفرع الرئيسي"),
                TotalAmount = total,
                Deposit = deposit,
                RemainingAmount = total - deposit,
                Comment = order.Comment,
                ReferenceNumber = order.ReferenceNumber,
                Items = items
            };
        }

        private static string FormatCustomerName(Customer c)
        {
            var fullName = $"{c.FirstName} {c.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName;
            }
            if (!string.IsNullOrWhiteSpace(c.Company))
            {
                return c.Company.Trim();
            }
            return "عميل بدون اسم";
        }
    }
}

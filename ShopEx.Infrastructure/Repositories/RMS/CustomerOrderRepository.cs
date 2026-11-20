using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Domain.RMSEntity;
using ShopEx.Infrastructure.Persistence.RMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class CustomerOrderRepository : ICustomerOrderRepository
    {
        private readonly RMSContext _context;

        public CustomerOrderRepository(RMSContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<(List<Customer> Items, int TotalCount, Dictionary<int, (int OpenCount, decimal OpenAmount, int MaxDaysOld)> Stats)> GetPagedCustomersAsync(
            string? search = null,
            int? minDaysOld = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            IQueryable<Customer> query = _context.Customers.AsNoTracking();

            // Filter customers who have open orders older than minDaysOld
            if (minDaysOld.HasValue && minDaysOld.Value > 0)
            {
                var threshold = DateTime.Today.AddDays(-minDaysOld.Value);
                var overdueCustomerIds = _context.Orders
                    .AsNoTracking()
                    .Where(o => (o.Closed == false || o.Closed == null) && o.Time != null && o.Time <= threshold && o.CustomerID != null)
                    .Select(o => o.CustomerID!.Value)
                    .Distinct();

                query = query.Where(c => overdueCustomerIds.Contains(c.ID));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(c =>
                    (c.AccountNumber != null && c.AccountNumber.Contains(term)) ||
                    (c.FirstName != null && c.FirstName.Contains(term)) ||
                    (c.LastName != null && c.LastName.Contains(term)) ||
                    (c.Company != null && c.Company.Contains(term)) ||
                    (c.PhoneNumber != null && c.PhoneNumber.Contains(term)));
            }

            int totalCount = await query.CountAsync(ct);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var customers = await query.OrderBy(c => c.FirstName)
                                       .ThenBy(c => c.LastName)
                                       .Skip((page - 1) * pageSize)
                                       .Take(pageSize)
                                       .ToListAsync(ct);

            // Fetch open orders stats for the returned customers
            var pageCustIds = customers.Select(c => c.ID).ToList();
            var openOrdersForPage = await _context.Orders
                .AsNoTracking()
                .Where(o => (o.Closed == false || o.Closed == null) && o.CustomerID != null && pageCustIds.Contains(o.CustomerID!.Value))
                .Select(o => new { o.CustomerID, o.Total, o.Deposit, o.Time })
                .ToListAsync(ct);

            var today = DateTime.Today;
            var stats = openOrdersForPage
                .GroupBy(o => o.CustomerID!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        OpenCount: g.Count(),
                        OpenAmount: g.Sum(x => (x.Total ?? 0) - (x.Deposit ?? 0)),
                        MaxDaysOld: g.Max(x => x.Time.HasValue ? (today - x.Time.Value.Date).Days : 0)
                    )
                );

            return (customers, totalCount, stats);
        }

        public Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default)
        {
            return _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.ID == customerId, ct);
        }

        public Task<Customer?> GetCustomerByAccountNumberAsync(string accountNumber, CancellationToken ct = default)
        {
            return _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.AccountNumber == accountNumber, ct);
        }

        public async Task<(List<Order> Items, int TotalCount)> GetPagedOpenOrdersAsync(
            int? customerId = null,
            int? storeId = null,
            int? minDaysOld = null,
            string? search = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default)
        {
            IQueryable<Order> query = _context.Orders
                .AsNoTracking()
                .Where(o => o.Closed == false || o.Closed == null);

            if (minDaysOld.HasValue && minDaysOld.Value > 0)
            {
                var threshold = DateTime.Today.AddDays(-minDaysOld.Value);
                query = query.Where(o => o.Time != null && o.Time <= threshold);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerID == customerId.Value);
            }

            if (storeId.HasValue)
            {
                query = query.Where(o => o.StoreID == storeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                var matchingCustIds = _context.Customers
                    .AsNoTracking()
                    .Where(c => (c.AccountNumber != null && c.AccountNumber.Contains(term)) ||
                                (c.FirstName != null && c.FirstName.Contains(term)) ||
                                (c.LastName != null && c.LastName.Contains(term)) ||
                                (c.Company != null && c.Company.Contains(term)) ||
                                (c.PhoneNumber != null && c.PhoneNumber.Contains(term)))
                    .Select(c => c.ID);

                query = query.Where(o => (o.CustomerID != null && matchingCustIds.Contains(o.CustomerID.Value)) ||
                                         (o.ReferenceNumber != null && o.ReferenceNumber.Contains(term)) ||
                                         (o.Comment != null && o.Comment.Contains(term)));
            }

            int totalCount = await query.CountAsync(ct);

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var orders = await query.OrderBy(o => o.Time)
                                    .ThenBy(o => o.ID)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync(ct);

            return (orders, totalCount);
        }

        public async Task<List<Order>> GetOpenOrdersByCustomerIdAsync(int customerId, int? minDaysOld = null, CancellationToken ct = default)
        {
            IQueryable<Order> query = _context.Orders
                .AsNoTracking()
                .Where(o => (o.Closed == false || o.Closed == null) && o.CustomerID == customerId);

            if (minDaysOld.HasValue && minDaysOld.Value > 0)
            {
                var threshold = DateTime.Today.AddDays(-minDaysOld.Value);
                query = query.Where(o => o.Time != null && o.Time <= threshold);
            }

            return await query.OrderBy(o => o.Time)
                              .ThenBy(o => o.ID)
                              .ToListAsync(ct);
        }

        public Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
        {
            return _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.ID == orderId, ct);
        }

        public Task<List<OrderEntry>> GetOrderEntriesAsync(int orderId, CancellationToken ct = default)
        {
            return _context.OrderEntries
                           .AsNoTracking()
                           .Where(e => e.OrderID == orderId)
                           .OrderBy(e => e.ID)
                           .ToListAsync(ct);
        }

        public Task<Store?> GetStoreByIdAsync(int storeId, CancellationToken ct = default)
        {
            return _context.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.ID == storeId, ct);
        }

        public async Task<Dictionary<int, string>> GetStoreNamesAsync(CancellationToken ct = default)
        {
            return await _context.Stores
                                 .AsNoTracking()
                                 .Select(s => new { s.ID, Name = s.Name ?? string.Empty })
                                 .ToDictionaryAsync(s => s.ID, s => s.Name, ct);
        }

        public async Task<Dictionary<int, (string Description, string LookupCode)>> GetItemInfoAsync(IEnumerable<int> itemIds, CancellationToken ct = default)
        {
            var distinctIds = itemIds.Distinct().ToList();
            var items = await _context.Items
                                      .AsNoTracking()
                                      .Where(i => distinctIds.Contains(i.ID))
                                      .Select(i => new
                                      {
                                          i.ID,
                                          Description = i.Description ?? string.Empty,
                                          LookupCode = i.ItemLookupCode ?? string.Empty
                                      })
                                      .ToListAsync(ct);

            return items.ToDictionary(i => i.ID, i => (i.Description, i.LookupCode));
        }

        public async Task<Dictionary<int, (string Name, string AccountNumber, string Phone)>> GetCustomerInfoAsync(IEnumerable<int> customerIds, CancellationToken ct = default)
        {
            var distinctIds = customerIds.Distinct().ToList();
            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => distinctIds.Contains(c.ID))
                .Select(c => new
                {
                    c.ID,
                    c.FirstName,
                    c.LastName,
                    c.Company,
                    AccountNumber = c.AccountNumber ?? string.Empty,
                    PhoneNumber = c.PhoneNumber ?? string.Empty
                })
                .ToListAsync(ct);

            return customers.ToDictionary(
                c => c.ID,
                c =>
                {
                    var fullName = $"{c.FirstName} {c.LastName}".Trim();
                    var name = !string.IsNullOrWhiteSpace(fullName)
                        ? fullName
                        : (!string.IsNullOrWhiteSpace(c.Company) ? c.Company.Trim() : "عميل بدون اسم");
                    return (name, c.AccountNumber, c.PhoneNumber);
                }
            );
        }
    }
}

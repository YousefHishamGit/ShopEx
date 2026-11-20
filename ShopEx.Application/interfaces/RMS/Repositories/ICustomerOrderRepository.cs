using ShopEx.Domain.RMSEntity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface ICustomerOrderRepository
    {
        Task<(List<Customer> Items, int TotalCount, Dictionary<int, (int OpenCount, decimal OpenAmount, int MaxDaysOld)> Stats)> GetPagedCustomersAsync(
            string? search = null,
            int? minDaysOld = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<Customer?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default);
        Task<Customer?> GetCustomerByAccountNumberAsync(string accountNumber, CancellationToken ct = default);

        Task<(List<Order> Items, int TotalCount)> GetPagedOpenOrdersAsync(
            int? customerId = null,
            int? storeId = null,
            int? minDaysOld = null,
            string? search = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<List<Order>> GetOpenOrdersByCustomerIdAsync(int customerId, int? minDaysOld = null, CancellationToken ct = default);
        Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);
        Task<List<OrderEntry>> GetOrderEntriesAsync(int orderId, CancellationToken ct = default);

        Task<Store?> GetStoreByIdAsync(int storeId, CancellationToken ct = default);
        Task<Dictionary<int, string>> GetStoreNamesAsync(CancellationToken ct = default);
        Task<Dictionary<int, (string Description, string LookupCode)>> GetItemInfoAsync(IEnumerable<int> itemIds, CancellationToken ct = default);
        Task<Dictionary<int, (string Name, string AccountNumber, string Phone)>> GetCustomerInfoAsync(IEnumerable<int> customerIds, CancellationToken ct = default);
    }
}

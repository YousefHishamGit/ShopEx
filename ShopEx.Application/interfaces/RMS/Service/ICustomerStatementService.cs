using ShopEx.Application.DTOs.RMS;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Service
{
    public interface ICustomerStatementService
    {
        Task<PagedResponse<CustomerSummaryDto>> GetPagedCustomersAsync(
            string? search = null,
            int? minDaysOld = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<PagedResponse<CustomerOpenInvoiceDto>> GetPagedOpenInvoicesAsync(
            int? customerId = null,
            int? storeId = null,
            int? minDaysOld = null,
            string? search = null,
            int page = 1,
            int pageSize = 20,
            CancellationToken ct = default);

        Task<CustomerStatementDto?> GetCustomerStatementAsync(
            int customerId,
            int? minDaysOld = null,
            CancellationToken ct = default);

        Task<CustomerStatementDto?> GetCustomerStatementByAccountAsync(
            string accountNumber,
            int? minDaysOld = null,
            CancellationToken ct = default);

        Task<CustomerInvoiceDetailDto?> GetInvoiceDetailAsync(
            int orderId,
            CancellationToken ct = default);
    }
}

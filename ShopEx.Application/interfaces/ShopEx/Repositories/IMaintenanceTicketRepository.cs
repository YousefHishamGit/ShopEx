using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IMaintenanceTicketRepository
    {
        Task<MaintenanceTicket?> GetByIdAsync(int id, CancellationToken ct = default);
        /// <summary>Same as <see cref="GetByIdAsync"/> but not tracked: for read-only screens, so a later
        /// locked update in the same request re-reads fresh values instead of reusing this instance.</summary>
        Task<MaintenanceTicket?> GetByIdReadOnlyAsync(int id, CancellationToken ct = default);
        Task<MaintenanceTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetAllAsync(int? storeId = null, CancellationToken ct = default);
        /// <summary>صفحة من التذاكر مع فلترة الحالة والبحث النصي، والتقسيم يتم في قاعدة البيانات.</summary>
        Task<(List<MaintenanceTicket> Items, int TotalCount)> GetPagedAsync(MaintenanceStatus? status, string? search, int? storeId, int skip, int take, CancellationToken ct = default);
        /// <summary>عدد التذاكر لكل حالة ضمن نطاق الفرع.</summary>
        /// <summary>إجمالي السعر على العميل وتكلفة القطع علينا لكل التذاكر المطابقة للفلاتر (تجميع داخل قاعدة البيانات)</summary>
        Task<(decimal Price, decimal PartCost)> GetCostTotalsAsync(MaintenanceStatus? status, string? search, int? storeId, CancellationToken ct = default);

        Task<Dictionary<MaintenanceStatus, int>> GetStatusCountsAsync(int? storeId, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetByStatusAsync(MaintenanceStatus status, int? storeId = null, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetByCustomerPhoneAsync(string customerPhone, int? storeId = null, CancellationToken ct = default);
        Task<bool> ExistsAsync(string ticketNumber, CancellationToken ct = default);
        Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default);
        void Update(MaintenanceTicket ticket);
        void Delete(MaintenanceTicket ticket);
        /// <summary>Next MT-yyyyMMdd-NNNN number (last number of the day + 1). Call it under the numbering lock.</summary>
        Task<string> GetNextTicketNumberAsync(CancellationToken ct = default);

     
        Task AddOperationAsync(MaintenanceTicketOperation operation, CancellationToken ct = default);
        Task<MaintenanceTicketOperation?> GetOperationByIdAsync(int operationId, CancellationToken ct = default);
        void RemoveOperation(MaintenanceTicketOperation operation);

        Task AddPaymentAsync(MaintenancePayment payment, CancellationToken ct = default);
    }
}

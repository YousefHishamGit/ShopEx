using ShopEx.Application.DTOs.Maintenance;
using ShopEx.Domain.Enum;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IMaintenanceTicketService
    {
        // المرحلة الأولى: إنشاء واستلام تذكرة الصيانة
        Task<MaintenanceTicketResponse> CreateAsync(CreateMaintenanceTicketRequest request, CancellationToken ct = default);

        // الاستعلامات
        Task<MaintenanceTicketResponse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<MaintenanceTicketResponse?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);
        Task<List<MaintenanceTicketSummaryResponse>> GetAllAsync(MaintenanceStatus? status = null, string? customerPhone = null, int? storeId = null, CancellationToken ct = default);

        Task<PagedMaintenanceTicketsResponse> GetPagedAsync(MaintenanceStatus? status, string? search, int? storeId, int page = 1, int pageSize = 25, CancellationToken ct = default);

        // المرحلة الثانية: تسجيل الإصلاحات والعمليات
        Task<MaintenanceTicketResponse> AddOperationAsync(int ticketId, AddTicketOperationRequest request, CancellationToken ct = default);
        Task<MaintenanceTicketResponse> DeleteOperationAsync(int ticketId, int operationId, CancellationToken ct = default);
        Task<MaintenanceTicketResponse> CompleteRepairAsync(int ticketId, CompleteRepairRequest? request = null, CancellationToken ct = default);

        // المرحلة الثالثة: الدفع والتسليم للعميل
        Task<MaintenanceTicketResponse> AddPaymentAsync(int ticketId, AddTicketPaymentRequest request, CancellationToken ct = default);
        /// <param name="branchStoreId">
        /// Set for a branch employee (their store from the token): the ticket must belong to that branch
        /// (else <see cref="KeyNotFoundException"/>), be ready (Completed) and the whole remaining amount must be collected.
        /// These rules are checked under the ticket lock together with the delivery. Null = HQ admin (no extra rules).
        /// </param>
        Task<MaintenanceTicketResponse> DeliverTicketAsync(int ticketId, DeliverTicketRequest? request = null, int? branchStoreId = null, CancellationToken ct = default);

        // تحديث الحالة يدوياً إن لزم
        Task<MaintenanceTicketResponse> UpdateStatusAsync(int ticketId, MaintenanceStatus newStatus, CancellationToken ct = default);
    }
}

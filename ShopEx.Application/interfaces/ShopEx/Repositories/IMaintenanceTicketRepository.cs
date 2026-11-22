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
        Task<MaintenanceTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetAllAsync(int? storeId = null, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetByStatusAsync(MaintenanceStatus status, int? storeId = null, CancellationToken ct = default);
        Task<List<MaintenanceTicket>> GetByCustomerPhoneAsync(string customerPhone, int? storeId = null, CancellationToken ct = default);
        Task<bool> ExistsAsync(string ticketNumber, CancellationToken ct = default);
        Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default);
        void Update(MaintenanceTicket ticket);
        void Delete(MaintenanceTicket ticket);
        Task<string> GetNextTicketNumberAsync(CancellationToken ct = default);

     
        Task AddOperationAsync(MaintenanceTicketOperation operation, CancellationToken ct = default);
        Task<MaintenanceTicketOperation?> GetOperationByIdAsync(int operationId, CancellationToken ct = default);
        void RemoveOperation(MaintenanceTicketOperation operation);

        Task AddPaymentAsync(MaintenancePayment payment, CancellationToken ct = default);
    }
}

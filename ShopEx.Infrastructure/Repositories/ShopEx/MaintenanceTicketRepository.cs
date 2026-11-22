using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class MaintenanceTicketRepository : IMaintenanceTicketRepository
    {
        private readonly ShopContext _db;

        public MaintenanceTicketRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<MaintenanceTicket?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .Include(t => t.Operations)
                      .Include(t => t.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public Task<MaintenanceTicket?> GetByTicketNumberAsync(string ticketNumber, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .Include(t => t.Operations)
                      .Include(t => t.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber, ct);
        }

        public Task<List<MaintenanceTicket>> GetAllAsync(int? storeId = null, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .Include(t => t.Operations)
                      .Include(t => t.Payments)
                      .AsSplitQuery()
                      .Where(t => storeId == null || t.StoreId == storeId)
                      .OrderByDescending(t => t.ReceivedAt)
                      .ToListAsync(ct);
        }

        public Task<List<MaintenanceTicket>> GetByStatusAsync(MaintenanceStatus status, int? storeId = null, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .Include(t => t.Operations)
                      .Include(t => t.Payments)
                      .AsSplitQuery()
                      .Where(t => t.Status == status && (storeId == null || t.StoreId == storeId))
                      .OrderByDescending(t => t.ReceivedAt)
                      .ToListAsync(ct);
        }

        public Task<List<MaintenanceTicket>> GetByCustomerPhoneAsync(string customerPhone, int? storeId = null, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .Include(t => t.Operations)
                      .Include(t => t.Payments)
                      .AsSplitQuery()
                      .Where(t => t.CustomerPhone.Contains(customerPhone) && (storeId == null || t.StoreId == storeId))
                      .OrderByDescending(t => t.ReceivedAt)
                      .ToListAsync(ct);
        }

        public Task<bool> ExistsAsync(string ticketNumber, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .AnyAsync(t => t.TicketNumber == ticketNumber, ct);
        }

        public async Task AddAsync(MaintenanceTicket ticket, CancellationToken ct = default)
        {
            await _db.MaintenanceTickets.AddAsync(ticket, ct);
        }

        public void Update(MaintenanceTicket ticket)
        {
            _db.MaintenanceTickets.Update(ticket);
        }

        public void Delete(MaintenanceTicket ticket)
        {
            _db.MaintenanceTickets.Remove(ticket);
        }

        public async Task<string> GetNextTicketNumberAsync(CancellationToken ct = default)
        {
            var todayPrefix = $"MT-{DateTime.Now:yyyyMMdd}-";
            var countToday = await _db.MaintenanceTickets
                                      .CountAsync(t => t.TicketNumber.StartsWith(todayPrefix), ct);

            return $"{todayPrefix}{(countToday + 1):D4}";
        }

        public async Task AddOperationAsync(MaintenanceTicketOperation operation, CancellationToken ct = default)
        {
            await _db.MaintenanceTicketOperations.AddAsync(operation, ct);
        }

        public Task<MaintenanceTicketOperation?> GetOperationByIdAsync(int operationId, CancellationToken ct = default)
        {
            return _db.MaintenanceTicketOperations
                      .FirstOrDefaultAsync(o => o.Id == operationId, ct);
        }

        public void RemoveOperation(MaintenanceTicketOperation operation)
        {
            _db.MaintenanceTicketOperations.Remove(operation);
        }

        public async Task AddPaymentAsync(MaintenancePayment payment, CancellationToken ct = default)
        {
            await _db.MaintenancePayments.AddAsync(payment, ct);
        }
    }
}

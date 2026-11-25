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

        public Task<MaintenanceTicket?> GetByIdReadOnlyAsync(int id, CancellationToken ct = default)
        {
            return _db.MaintenanceTickets
                      .AsNoTracking()
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

        private IQueryable<MaintenanceTicket> Filtered(MaintenanceStatus? status, string? search, int? storeId)
        {
            var query = _db.MaintenanceTickets.AsNoTracking()
                .Where(t => storeId == null || t.StoreId == storeId);

            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(t =>
                    t.TicketNumber.Contains(s) ||
                    t.CustomerName.Contains(s) ||
                    t.CustomerPhone.Contains(s) ||
                    t.DeviceName.Contains(s));
            }

            return query;
        }

        public async Task<(decimal Price, decimal PartCost)> GetCostTotalsAsync(
            MaintenanceStatus? status, string? search, int? storeId, CancellationToken ct = default)
        {
            var row = await Filtered(status, search, storeId)
                .SelectMany(t => t.Operations)
                .GroupBy(o => 1)
                .Select(g => new { Price = g.Sum(o => o.Cost), PartCost = g.Sum(o => o.PartCost) })
                .FirstOrDefaultAsync(ct);

            return (row?.Price ?? 0m, row?.PartCost ?? 0m);
        }

        public async Task<(List<MaintenanceTicket> Items, int TotalCount)> GetPagedAsync(
            MaintenanceStatus? status, string? search, int? storeId, int skip, int take, CancellationToken ct = default)
        {
            var query = Filtered(status, search, storeId);

            var total = await query.CountAsync(ct);
            var items = await query
                .OrderByDescending(t => t.ReceivedAt).ThenByDescending(t => t.Id)
                .Skip(skip).Take(take)
                .Include(t => t.Operations)
                .Include(t => t.Payments)
                .AsSplitQuery()
                .ToListAsync(ct);

            return (items, total);
        }

        public async Task<Dictionary<MaintenanceStatus, int>> GetStatusCountsAsync(int? storeId, CancellationToken ct = default)
        {
            var rows = await _db.MaintenanceTickets.AsNoTracking()
                .Where(t => storeId == null || t.StoreId == storeId)
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.Status, r => r.Count);
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
            // Last number of the day + 1 (a count would repeat a number if a ticket were ever removed)
            var todayPrefix = $"MT-{DateTime.Now:yyyyMMdd}-";
            var last = await _db.MaintenanceTickets
                                .Where(t => t.TicketNumber.StartsWith(todayPrefix))
                                .OrderByDescending(t => t.TicketNumber)
                                .Select(t => t.TicketNumber)
                                .FirstOrDefaultAsync(ct);

            var next = 1;
            if (last != null && int.TryParse(last[todayPrefix.Length..], out var seq))
                next = seq + 1;

            return $"{todayPrefix}{next:D4}";
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

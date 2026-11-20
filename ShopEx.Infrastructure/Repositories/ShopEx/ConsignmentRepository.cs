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
    public class ConsignmentRepository : IConsignmentRepository
    {
        private readonly ShopContext _db;

        public ConsignmentRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<Consignment?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Consignments
                      .Include(c => c.Dealer)
                      .Include(c => c.Items)
                          .ThenInclude(i => i.SettlementItems)
                      .Include(c => c.Settlements)
                          .ThenInclude(s => s.Items)
                      .Include(c => c.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(c => c.ID == id, ct);
        }

        public Task<Consignment?> GetByConsignmentNumberAsync(string consignmentNumber, CancellationToken ct = default)
        {
            return _db.Consignments
                      .Include(c => c.Dealer)
                      .Include(c => c.Items)
                          .ThenInclude(i => i.SettlementItems)
                      .Include(c => c.Settlements)
                          .ThenInclude(s => s.Items)
                      .Include(c => c.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(c => c.ConsignmentNumber == consignmentNumber, ct);
        }

        public Task<List<Consignment>> GetAllAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            IQueryable<Consignment> query = _db.Consignments
                                               .Include(c => c.Dealer)
                                               .Include(c => c.Items)
                                                   .ThenInclude(i => i.SettlementItems)
                                               .Include(c => c.Settlements)
                                                   .ThenInclude(s => s.Items)
                                               .Include(c => c.Payments)
                                               .AsSplitQuery();

            if (dealerId.HasValue)
            {
                query = query.Where(c => c.DealerID == dealerId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(c => c.Status == status.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(c => c.IssueDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(c => c.IssueDate <= toDate.Value);
            }

            return query.OrderByDescending(c => c.IssueDate)
                        .ThenByDescending(c => c.ID)
                        .ToListAsync(ct);
        }

        public Task<bool> ExistsAsync(string consignmentNumber, CancellationToken ct = default)
        {
            return _db.Consignments.AnyAsync(c => c.ConsignmentNumber == consignmentNumber, ct);
        }

        public async Task<string> GetNextConsignmentNumberAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            var prefix = $"CSG-{today:yyyyMMdd}-";

            var lastConsignment = await _db.Consignments
                                          .Where(c => c.ConsignmentNumber.StartsWith(prefix))
                                          .OrderByDescending(c => c.ConsignmentNumber)
                                          .Select(c => c.ConsignmentNumber)
                                          .FirstOrDefaultAsync(ct);

            int nextSequence = 1;
            if (!string.IsNullOrEmpty(lastConsignment))
            {
                var parts = lastConsignment.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int seq))
                {
                    nextSequence = seq + 1;
                }
            }

            return $"{prefix}{nextSequence:D4}";
        }

        public async Task AddAsync(Consignment consignment, CancellationToken ct = default)
        {
            await _db.Consignments.AddAsync(consignment, ct);
        }

        public void Update(Consignment consignment)
        {
            _db.Consignments.Update(consignment);
        }

        public async Task AddItemAsync(ConsignmentItem item, CancellationToken ct = default)
        {
            await _db.ConsignmentItems.AddAsync(item, ct);
        }

        public void RemoveItem(ConsignmentItem item)
        {
            _db.ConsignmentItems.Remove(item);
        }

        public async Task AddSettlementAsync(ConsignmentSettlement settlement, CancellationToken ct = default)
        {
            await _db.ConsignmentSettlements.AddAsync(settlement, ct);
        }

        public Task<ConsignmentSettlement?> GetSettlementByIdAsync(int settlementId, CancellationToken ct = default)
        {
            return _db.ConsignmentSettlements
                      .Include(s => s.Items)
                          .ThenInclude(si => si.ConsignmentItem)
                      .FirstOrDefaultAsync(s => s.ID == settlementId, ct);
        }

        public async Task AddPaymentAsync(ConsignmentPayment payment, CancellationToken ct = default)
        {
            await _db.ConsignmentPayments.AddAsync(payment, ct);
        }
    }
}

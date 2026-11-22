using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Consignment;
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

            query = ApplyFilters(query, dealerId, status, fromDate, toDate);

            return query.OrderByDescending(c => c.IssueDate)
                        .ThenByDescending(c => c.ID)
                        .ToListAsync(ct);
        }

        /// <summary>
        /// Lightweight list query: aggregates are computed in SQL and no entity graph is materialized.
        /// </summary>
        public async Task<List<ConsignmentSummaryResponse>> GetSummariesAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var query = ApplyFilters(_db.Consignments.AsNoTracking(), dealerId, status, fromDate, toDate);

            var rows = await query
                .OrderByDescending(c => c.IssueDate)
                .ThenByDescending(c => c.ID)
                .Select(c => new
                {
                    c.ID,
                    c.ConsignmentNumber,
                    c.DealerID,
                    DealerName = c.Dealer.Name,
                    DealerPhone = c.Dealer.Phone,
                    c.IssueDate,
                    c.AgreedDays,
                    c.DueDate,
                    c.Status,
                    ItemsCount = c.Items.Count,
                    Issued = c.Items.Sum(i => i.QuantityIssued * i.UnitPrice),
                    Sold = c.Items.Sum(i => i.SettlementItems.Sum(s => s.QuantitySold) * i.UnitPrice),
                    OutQty = c.Items.Sum(i => i.QuantityIssued - i.SettlementItems.Sum(s => s.QuantitySold + s.QuantityReturned)),
                    OutValue = c.Items.Sum(i => (i.QuantityIssued - i.SettlementItems.Sum(s => s.QuantitySold + s.QuantityReturned)) * i.UnitPrice),
                    Paid = c.Payments.Sum(p => p.Amount)
                })
                .ToListAsync(ct);

            var today = DateTime.Today;
            return rows.Select(r =>
            {
                var remaining = r.Sold - r.Paid;
                return new ConsignmentSummaryResponse
                {
                    ID = r.ID,
                    ConsignmentNumber = r.ConsignmentNumber,
                    DealerID = r.DealerID,
                    DealerName = r.DealerName ?? string.Empty,
                    DealerPhone = r.DealerPhone ?? string.Empty,
                    IssueDate = r.IssueDate,
                    AgreedDays = r.AgreedDays,
                    DueDate = r.DueDate,
                    Status = r.Status,
                    ItemsCount = r.ItemsCount,
                    TotalIssuedValue = r.Issued,
                    TotalSoldValue = r.Sold,
                    PaidAmount = r.Paid,
                    RemainingAmount = remaining,
                    OutstandingQuantity = r.OutQty,
                    OutstandingValue = r.OutValue,
                    IsFullyAccountedFor = r.OutQty <= 0,
                    IsOverdue = r.Status == ConsignmentStatus.Open
                                && r.DueDate.Date < today
                                && (r.OutQty > 0 || remaining > 0)
                };
            }).ToList();
        }

        private static IQueryable<Consignment> ApplyFilters(
            IQueryable<Consignment> query,
            int? dealerId,
            ConsignmentStatus? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (dealerId.HasValue)
                query = query.Where(c => c.DealerID == dealerId.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(c => c.IssueDate >= from);
            }

            if (toDate.HasValue)
            {
                // inclusive of the whole "to" day
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(c => c.IssueDate < toExclusive);
            }

            return query;
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

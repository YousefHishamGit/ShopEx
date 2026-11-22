using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ShopContext _db;

        public DashboardRepository(ShopContext db)
        {
            _db = db;
        }

        public async Task<DailyShopActivity> GetDailyActivityAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var result = new DailyShopActivity();

            // ----- الأجهزة -----
            result.TicketsReceived = await _db.MaintenanceTickets.AsNoTracking()
                .Where(t => t.ReceivedAt >= from && t.ReceivedAt < to)
                .GroupBy(t => t.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            result.TicketsDelivered = await _db.MaintenanceTickets.AsNoTracking()
                .Where(t => t.DeliveredAt != null && t.DeliveredAt >= from && t.DeliveredAt < to)
                .GroupBy(t => t.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            result.TicketsRepaired = await _db.MaintenanceTickets.AsNoTracking()
                .CountAsync(t => t.RepairedAt != null && t.RepairedAt >= from && t.RepairedAt < to, ct);

            result.MaintenanceRevenue = await _db.MaintenancePayments.AsNoTracking()
                .Where(p => p.PaidAt >= from && p.PaidAt < to)
                .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

            // الحالة الحالية (لا تتأثر بتاريخ اللوحة)
            result.UnderRepairNow = await _db.MaintenanceTickets.AsNoTracking()
                .CountAsync(t => t.Status == MaintenanceStatus.Received || t.Status == MaintenanceStatus.UnderRepair, ct);
            result.ReadyForDeliveryNow = await _db.MaintenanceTickets.AsNoTracking()
                .CountAsync(t => t.Status == MaintenanceStatus.Completed, ct);

            // ----- المصروفات -----
            result.Expenses = await _db.Expenses.AsNoTracking()
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to)
                .GroupBy(e => e.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(e => e.Amount), Count = g.Count() })
                .ToListAsync(ct);

            // ----- الأرصدة -----
            result.BalanceClosures = await _db.BalanceClosures.AsNoTracking()
                .Where(c => c.ClosedAt >= from && c.ClosedAt < to)
                .GroupBy(c => (int?)c.Balance.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(c => c.AmountUsed), Count = g.Count() })
                .ToListAsync(ct);

            result.BalanceCharged = await _db.Balances.AsNoTracking()
                .Where(b => b.ChargedAt >= from && b.ChargedAt < to)
                .SumAsync(b => (decimal?)b.Amount, ct) ?? 0m;

            return result;
        }

        public async Task<List<UnpaidInvoiceRow>> GetUnpaidDatedInvoicesAsync(CancellationToken ct = default)
        {
            return await _db.SupplierInvoices.AsNoTracking()
                .Where(i => i.DueDate != null)
                .Select(i => new UnpaidInvoiceRow
                {
                    InvoiceId = i.ID,
                    InvoiceNumber = i.InvoiceNumber,
                    SupplierId = i.RMSSupplierID,
                    DueDate = i.DueDate,
                    TotalAmount = i.TotalAmount,
                    PaidAmount = i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .Where(r => r.TotalAmount - r.PaidAmount > 0)
                .ToListAsync(ct);
        }

        public async Task<decimal> GetTotalOutstandingInvoicesAsync(CancellationToken ct = default)
        {
            var rows = await _db.SupplierInvoices.AsNoTracking()
                .Select(i => new
                {
                    Remaining = i.TotalAmount - (i.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .Where(r => r.Remaining > 0)
                .Select(r => r.Remaining)
                .ToListAsync(ct);

            return rows.Sum();
        }
    }
}

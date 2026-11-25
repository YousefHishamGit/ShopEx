using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.Services.ShopEx;
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

            result.MaintenanceRevenueByStore = await _db.MaintenancePayments.AsNoTracking()
                .Where(p => p.PaidAt >= from && p.PaidAt < to)
                .GroupBy(p => p.MaintenanceTicket.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
                .ToListAsync(ct);
            result.MaintenanceRevenue = result.MaintenanceRevenueByStore.Sum(r => r.Amount);

            result.MaintenanceProfitByStore = await _db.MaintenanceTicketOperations.AsNoTracking()
                .Where(o => o.MaintenanceTicket.Status == MaintenanceStatus.Delivered
                            && o.MaintenanceTicket.DeliveredAt != null
                            && o.MaintenanceTicket.DeliveredAt >= from && o.MaintenanceTicket.DeliveredAt < to)
                .GroupBy(o => o.MaintenanceTicket.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(o => o.Cost), Profit = g.Sum(o => o.Cost - o.PartCost) })
                .ToListAsync(ct);

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

            var selfCharge = BalanceService.SelfChargeExpenseCategory;
            result.OperatingExpenses = await _db.Expenses.AsNoTracking()
                .Where(e => e.ExpenseDate >= from && e.ExpenseDate < to
                            && e.DeviceId == null && e.BalanceId == null
                            && e.ExpenseCategory.Name != selfCharge)
                .GroupBy(e => e.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(e => e.Amount), Count = g.Count() })
                .ToListAsync(ct);

            // ----- البنزيمات: بضاعة حُوسبت (بيع) على التجار في اليوم -----
            var consignmentSold = await _db.ConsignmentSettlementItems.AsNoTracking()
                .Where(i => i.ConsignmentSettlement.SettlementDate >= from && i.ConsignmentSettlement.SettlementDate < to)
                .GroupBy(i => 1)
                .Select(g => new
                {
                    Revenue = g.Sum(i => i.QuantitySold * i.ConsignmentItem.UnitPrice),
                    Cost = g.Sum(i => i.QuantitySold * i.ConsignmentItem.UnitCost)
                })
                .FirstOrDefaultAsync(ct);
            result.ConsignmentRevenue = consignmentSold?.Revenue ?? 0m;
            result.ConsignmentCost = consignmentSold?.Cost ?? 0m;

            // ----- الأرصدة -----
            result.BalanceClosures = await _db.BalanceClosures.AsNoTracking()
                .Where(c => c.ClosedAt >= from && c.ClosedAt < to)
                .GroupBy(c => (int?)c.Balance.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(c => c.AmountUsed), Profit = g.Sum(c => c.Profit), Count = g.Count() })
                .ToListAsync(ct);

            result.BalanceChargedByStore = await _db.Balances.AsNoTracking()
                .Where(b => b.ChargedAt >= from && b.ChargedAt < to)
                .GroupBy(b => (int?)b.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(b => b.Amount), Count = g.Count() })
                .ToListAsync(ct);
            result.BalanceCharged = result.BalanceChargedByStore.Sum(r => r.Amount);

            // المتبقي الآن = كل الشحنات - كل التقفيلات (لكل فرع)
            var chargedAll = await _db.Balances.AsNoTracking()
                .GroupBy(b => b.StoreId)
                .Select(g => new { StoreId = g.Key, Amount = g.Sum(b => b.Amount) })
                .ToListAsync(ct);
            var usedAll = await _db.BalanceClosures.AsNoTracking()
                .GroupBy(c => c.Balance.StoreId)
                .Select(g => new { StoreId = g.Key, Amount = g.Sum(c => c.AmountUsed) })
                .ToListAsync(ct);
            var usedByStore = usedAll.ToDictionary(x => x.StoreId, x => x.Amount);
            result.BalanceRemainingByStore = chargedAll
                .Select(x => new StoreAmountRow
                {
                    StoreId = x.StoreId,
                    Amount = x.Amount - (usedByStore.TryGetValue(x.StoreId, out var u) ? u : 0m)
                })
                .ToList();

            // ----- الأجهزة المشتراة بغرض البيع -----
            result.DevicesBought = await _db.Devices.AsNoTracking()
                .Where(d => d.PurchaseDate >= from && d.PurchaseDate < to)
                .GroupBy(d => d.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(d => d.PurchasePrice), Count = g.Count() })
                .ToListAsync(ct);

            result.DevicesSold = await _db.Devices.AsNoTracking()
                .Where(d => d.Status == DeviceStatus.Sold && d.SoldAt != null && d.SoldAt >= from && d.SoldAt < to)
                .GroupBy(d => d.SoldStoreId)
                .Select(g => new StoreAmountRow
                {
                    StoreId = g.Key,
                    Amount = g.Sum(d => d.SalePrice ?? 0m),
                    Profit = g.Sum(d => (d.SalePrice ?? 0m) - d.PurchasePrice),
                    Count = g.Count()
                })
                .ToListAsync(ct);

            result.DevicesInStock = await _db.Devices.AsNoTracking()
                .Where(d => d.Status == DeviceStatus.InStock)
                .GroupBy(d => d.StoreId)
                .Select(g => new StoreAmountRow { StoreId = g.Key, Amount = g.Sum(d => d.PurchasePrice), Count = g.Count() })
                .ToListAsync(ct);

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

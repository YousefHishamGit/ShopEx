using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class DashboardService : IDashboardService
    {
        /// <summary>الفواتير/الأمانات التي يحل موعدها خلال هذا العدد من الأيام تُعتبر "قريبة الاستحقاق".</summary>
        public const int DueSoonDays = 7;

        /// <summary>أقصى عدد صفوف في قائمة المستحقات المعروضة (العدادات والإجماليات تشمل الكل).</summary>
        private const int MaxListItems = 15;

        private readonly ISalesRepository _salesRepo;
        private readonly IDashboardRepository _dashboardRepo;
        private readonly IStoreRepository _storeRepo;
        private readonly ISupplierRepository _supplierRepo;
        private readonly IConsignmentRepository _consignmentRepo;

        public DashboardService(
            ISalesRepository salesRepo,
            IDashboardRepository dashboardRepo,
            IStoreRepository storeRepo,
            ISupplierRepository supplierRepo,
            IConsignmentRepository consignmentRepo)
        {
            _salesRepo = salesRepo ?? throw new ArgumentNullException(nameof(salesRepo));
            _dashboardRepo = dashboardRepo ?? throw new ArgumentNullException(nameof(dashboardRepo));
            _storeRepo = storeRepo ?? throw new ArgumentNullException(nameof(storeRepo));
            _supplierRepo = supplierRepo ?? throw new ArgumentNullException(nameof(supplierRepo));
            _consignmentRepo = consignmentRepo ?? throw new ArgumentNullException(nameof(consignmentRepo));
        }

        public async Task<DashboardResponse> GetAsync(DateTime? date = null, CancellationToken ct = default)
        {
            var now = DateTime.Now;
            var day = (date ?? now).Date;
            var from = day;
            var to = day.AddDays(1);

            // تباعاً (وليس بالتوازي): كل DbContext لا يدعم أكثر من استعلام في نفس الوقت.
            var sales = await _salesRepo.GetSalesByStoreAsync(from, to, ct);
            var activity = await _dashboardRepo.GetDailyActivityAsync(from, to, ct);
            var storeNames = await _storeRepo.GetNamesAsync(ct);

            var response = new DashboardResponse
            {
                Date = day,
                GeneratedAt = now,
                Sales = BuildSales(sales),
                Maintenance = new MaintenanceSummaryDto
                {
                    ReceivedToday = activity.TicketsReceived.Sum(x => x.Count),
                    DeliveredToday = activity.TicketsDelivered.Sum(x => x.Count),
                    RepairedToday = activity.TicketsRepaired,
                    RevenueToday = activity.MaintenanceRevenue,
                    UnderRepairNow = activity.UnderRepairNow,
                    ReadyForDeliveryNow = activity.ReadyForDeliveryNow
                },
                Expenses = new ExpensesSummaryDto
                {
                    Total = activity.Expenses.Sum(x => x.Amount),
                    Count = activity.Expenses.Sum(x => x.Count)
                },
                Balances = new BalancesSummaryDto
                {
                    ChargedToday = activity.BalanceCharged,
                    ClosedToday = activity.BalanceClosures.Sum(x => x.Amount),
                    ProfitToday = activity.BalanceClosures.Sum(x => x.Profit)
                },
                Branches = BuildBranches(sales, activity, storeNames)
            };

            // المستحقات تُحسب دائماً بالنسبة لليوم الفعلي، مهما كان اليوم المعروض.
            response.SupplierInvoices = await BuildInvoiceDuesAsync(now.Date, ct);
            response.Consignments = await BuildConsignmentDuesAsync(now.Date, ct);

            return response;
        }

        // ================================================================
        // المبيعات والفروع
        // ================================================================

        private static SalesSummaryDto BuildSales(List<StoreSalesRow> rows)
        {
            var total = rows.Sum(r => r.Sales);
            var cost = rows.Sum(r => r.Cost);
            var profit = total - cost;

            return new SalesSummaryDto
            {
                TotalSales = total,
                TotalCost = cost,
                GrossProfit = profit,
                MarginPercent = total > 0 ? Math.Round(profit / total * 100m, 1) : 0m,
                TransactionsCount = rows.Sum(r => r.TransactionsCount),
                ItemsSold = rows.Sum(r => r.ItemsSold)
            };
        }

        /// <summary>مفتاح الحركات غير المرتبطة بفرع (StoreId = null) لأن القاموس لا يقبل مفتاحاً null.</summary>
        private const int NoStore = -1;

        private static List<BranchDailyDto> BuildBranches(
            List<StoreSalesRow> sales,
            DailyShopActivity activity,
            Dictionary<int, string> storeNames)
        {
            var salesByStore = sales.ToDictionary(s => s.StoreId);
            var received = ByStore(activity.TicketsReceived);
            var delivered = ByStore(activity.TicketsDelivered);
            var expenses = ByStore(activity.Expenses);
            var closures = ByStore(activity.BalanceClosures);

            // كل فروع RMS (حتى التي لم تتحرك اليوم) + أي فرع له حركة وغير موجود في القائمة
            var storeIds = new HashSet<int>(storeNames.Keys);
            storeIds.UnionWith(salesByStore.Keys);
            foreach (var lookup in new[] { received, delivered, expenses, closures })
                storeIds.UnionWith(lookup.Keys);

            var rows = new List<BranchDailyDto>();
            foreach (var id in storeIds)
            {
                salesByStore.TryGetValue(id, out var s);

                var row = new BranchDailyDto
                {
                    StoreId = id == NoStore ? null : id,
                    StoreName = id == NoStore
                        ? "غير مرتبط بفرع (الإدارة)"
                        : (storeNames.TryGetValue(id, out var name) ? name : $"فرع #{id}"),
                    Sales = s?.Sales ?? 0m,
                    Profit = s == null ? 0m : s.Sales - s.Cost,
                    TransactionsCount = s?.TransactionsCount ?? 0,
                    DevicesReceived = received.TryGetValue(id, out var r) ? r.Count : 0,
                    DevicesDelivered = delivered.TryGetValue(id, out var d) ? d.Count : 0,
                    Expenses = expenses.TryGetValue(id, out var e) ? e.Amount : 0m,
                    BalanceClosed = closures.TryGetValue(id, out var c) ? c.Amount : 0m,
                    BalanceProfit = c?.Profit ?? 0m
                };

                // الصف "غير المرتبط بفرع" يظهر فقط لو فيه حركة فعلية
                if (id == NoStore && row.DevicesReceived == 0 && row.DevicesDelivered == 0
                    && row.Expenses == 0 && row.BalanceClosed == 0)
                    continue;

                rows.Add(row);
            }

            return rows
                .OrderByDescending(r => r.Sales)
                .ThenByDescending(r => r.DevicesReceived)
                .ThenBy(r => r.StoreName)
                .ToList();
        }

        private static Dictionary<int, StoreAmountRow> ByStore(List<StoreAmountRow> rows)
            => rows.GroupBy(r => r.StoreId ?? NoStore)
                   .ToDictionary(g => g.Key, g => new StoreAmountRow
                   {
                       StoreId = g.Key == NoStore ? null : g.Key,
                       Amount = g.Sum(x => x.Amount),
                       Profit = g.Sum(x => x.Profit),
                       Count = g.Sum(x => x.Count)
                   });

        // ================================================================
        // فواتير الموردين (فترة السماح)
        // ================================================================

        private async Task<InvoiceDuesDto> BuildInvoiceDuesAsync(DateTime today, CancellationToken ct)
        {
            var dto = new InvoiceDuesDto { DueSoonDays = DueSoonDays };

            dto.TotalOutstanding = await _dashboardRepo.GetTotalOutstandingInvoicesAsync(ct);
            var unpaid = await _dashboardRepo.GetUnpaidDatedInvoicesAsync(ct);

            var due = unpaid
                .Select(i => new
                {
                    Row = i,
                    Remaining = i.TotalAmount - i.PaidAmount,
                    DaysToDue = (i.DueDate!.Value.Date - today).Days
                })
                .Where(x => x.DaysToDue <= DueSoonDays)
                .ToList();

            var overdue = due.Where(x => x.DaysToDue < 0).ToList();
            var dueToday = due.Where(x => x.DaysToDue == 0).ToList();
            var dueSoon = due.Where(x => x.DaysToDue > 0).ToList();

            dto.OverdueCount = overdue.Count;
            dto.OverdueAmount = overdue.Sum(x => x.Remaining);
            dto.DueTodayCount = dueToday.Count;
            dto.DueTodayAmount = dueToday.Sum(x => x.Remaining);
            dto.DueSoonCount = dueSoon.Count;
            dto.DueSoonAmount = dueSoon.Sum(x => x.Remaining);

            var listed = due.OrderBy(x => x.Row.DueDate).ThenBy(x => x.Row.InvoiceId).Take(MaxListItems).ToList();
            var suppliers = listed.Count == 0
                ? new Dictionary<int, Supplier>()
                : await _supplierRepo.GetByIdsAsync(listed.Select(x => x.Row.SupplierId));

            dto.Items = listed.Select(x => new DueInvoiceDto
            {
                InvoiceId = x.Row.InvoiceId,
                InvoiceNumber = x.Row.InvoiceNumber,
                SupplierId = x.Row.SupplierId,
                SupplierName = suppliers.TryGetValue(x.Row.SupplierId, out var s) && !string.IsNullOrWhiteSpace(s.SupplierName)
                    ? s.SupplierName
                    : $"مورد #{x.Row.SupplierId}",
                DueDate = x.Row.DueDate!.Value,
                TotalAmount = x.Row.TotalAmount,
                PaidAmount = x.Row.PaidAmount,
                RemainingAmount = x.Remaining,
                DaysToDue = x.DaysToDue
            }).ToList();

            return dto;
        }

        // ================================================================
        // أمانات التجار (البنزيمات)
        // ================================================================

        private async Task<ConsignmentDuesDto> BuildConsignmentDuesAsync(DateTime today, CancellationToken ct)
        {
            var dto = new ConsignmentDuesDto { DueSoonDays = DueSoonDays };

            var open = await _consignmentRepo.GetSummariesAsync(status: ConsignmentStatus.Open, ct: ct);

            var overdue = open.Where(c => c.IsOverdue).ToList();
            var soon = open
                .Where(c => !c.IsOverdue
                            && c.DueDate.Date >= today
                            && c.DueDate.Date <= today.AddDays(DueSoonDays)
                            && (c.OutstandingQuantity > 0 || c.RemainingAmount > 0))
                .ToList();

            dto.OverdueCount = overdue.Count;
            dto.OverdueMoney = overdue.Where(c => c.RemainingAmount > 0).Sum(c => c.RemainingAmount);
            dto.OverdueGoodsValue = overdue.Where(c => c.OutstandingValue > 0).Sum(c => c.OutstandingValue);
            dto.DueSoonCount = soon.Count;

            dto.Items = overdue.Concat(soon)
                .OrderBy(c => c.DueDate)
                .ThenBy(c => c.ID)
                .Take(MaxListItems)
                .Select(c => new DueConsignmentDto
                {
                    ConsignmentId = c.ID,
                    ConsignmentNumber = c.ConsignmentNumber,
                    DealerName = c.DealerName,
                    DealerPhone = c.DealerPhone,
                    DueDate = c.DueDate,
                    RemainingMoney = Math.Max(0m, c.RemainingAmount),
                    OutstandingGoodsValue = Math.Max(0m, c.OutstandingValue),
                    DaysToDue = (c.DueDate.Date - today).Days
                })
                .ToList();

            return dto;
        }
    }
}

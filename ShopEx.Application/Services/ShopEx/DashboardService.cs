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

            (response.Pnl, response.PnlTotal) = BuildPnl(sales, activity, storeNames);

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
            var maintRevenue = ByStore(activity.MaintenanceRevenueByStore);
            var maintProfit = ByStore(activity.MaintenanceProfitByStore);
            var expenses = ByStore(activity.Expenses);
            var closures = ByStore(activity.BalanceClosures);
            var charged = ByStore(activity.BalanceChargedByStore);
            var remaining = ByStore(activity.BalanceRemainingByStore);
            var bought = ByStore(activity.DevicesBought);
            var sold = ByStore(activity.DevicesSold);
            var inStock = ByStore(activity.DevicesInStock);

            // كل فروع RMS (حتى التي لم تتحرك اليوم) + أي فرع له حركة وغير موجود في القائمة
            var storeIds = new HashSet<int>(storeNames.Keys);
            storeIds.UnionWith(salesByStore.Keys);
            foreach (var lookup in new[] { received, delivered, maintRevenue, maintProfit, expenses, closures, charged, remaining, bought, sold, inStock })
                storeIds.UnionWith(lookup.Keys);

            // صف الإدارة العامة يظهر دائماً (البنزيمات والمصروفات والأجهزة غير المرتبطة بفرع)
            storeIds.Add(NoStore);

            var rows = new List<BranchDailyDto>();
            foreach (var id in storeIds)
            {
                salesByStore.TryGetValue(id, out var s);
                received.TryGetValue(id, out var r);
                delivered.TryGetValue(id, out var d);
                maintRevenue.TryGetValue(id, out var mr);
                maintProfit.TryGetValue(id, out var mp);
                expenses.TryGetValue(id, out var e);
                closures.TryGetValue(id, out var c);
                charged.TryGetValue(id, out var ch);
                remaining.TryGetValue(id, out var rem);
                bought.TryGetValue(id, out var b);
                sold.TryGetValue(id, out var so);
                inStock.TryGetValue(id, out var ins);

                rows.Add(new BranchDailyDto
                {
                    StoreId = id == NoStore ? null : id,
                    StoreName = id == NoStore
                        ? "الإدارة العامة (HQ)"
                        : (storeNames.TryGetValue(id, out var name) ? name : $"فرع #{id}"),
                    Sales = s?.Sales ?? 0m,
                    Profit = s == null ? 0m : s.Sales - s.Cost,
                    TransactionsCount = s?.TransactionsCount ?? 0,
                    TicketsReceived = r?.Count ?? 0,
                    TicketsDelivered = d?.Count ?? 0,
                    MaintenanceRevenue = mr?.Amount ?? 0m,
                    MaintenanceProfit = mp?.Profit ?? 0m,
                    DevicesBoughtCount = b?.Count ?? 0,
                    DevicesBoughtAmount = b?.Amount ?? 0m,
                    DevicesSoldCount = so?.Count ?? 0,
                    DevicesSoldAmount = so?.Amount ?? 0m,
                    DevicesProfit = so?.Profit ?? 0m,
                    DevicesInStockCount = ins?.Count ?? 0,
                    DevicesInStockValue = ins?.Amount ?? 0m,
                    BalanceCharged = ch?.Amount ?? 0m,
                    BalanceClosed = c?.Amount ?? 0m,
                    BalanceProfit = c?.Profit ?? 0m,
                    BalanceRemaining = rem?.Amount ?? 0m,
                    Expenses = e?.Amount ?? 0m
                });
            }

            // الفروع أولاً (بالمبيعات)، وصف الإدارة العامة في الآخر
            return rows
                .OrderBy(r => r.StoreId == null ? 1 : 0)
                .ThenByDescending(r => r.Sales)
                .ThenBy(r => r.StoreName)
                .ToList();
        }

        /// <summary>
        /// جدول الإيرادات / التكلفة / الأرباح / المصروفات / الصافي:
        /// صف لكل فرع (مبيعات RMS) ثم الصيانة والأجهزة والرصيد والبنزيمات. المصروفات هنا تشغيلية فقط
        /// (شراء الأجهزة وشحن الرصيد يظهران كتكلفة في صفيهما حتى لا يُحسبا مرتين).
        /// </summary>
        private static (List<PnlRowDto> Rows, PnlRowDto Total) BuildPnl(
            List<StoreSalesRow> sales,
            DailyShopActivity activity,
            Dictionary<int, string> storeNames)
        {
            var salesByStore = sales.ToDictionary(s => s.StoreId);
            var opExpenses = ByStore(activity.OperatingExpenses);

            var storeIds = new HashSet<int>(storeNames.Keys);
            storeIds.UnionWith(salesByStore.Keys);
            storeIds.UnionWith(opExpenses.Keys.Where(k => k != NoStore));

            PnlRowDto Make(string kind, int? storeId, string label, decimal revenue, decimal cost, decimal expenses = 0m) => new()
            {
                Kind = kind,
                StoreId = storeId,
                Label = label,
                Revenue = revenue,
                Cost = cost,
                Profit = revenue - cost,
                Expenses = expenses,
                Net = revenue - cost - expenses
            };

            var rows = new List<PnlRowDto>();

            foreach (var id in storeIds
                         .OrderByDescending(i => salesByStore.TryGetValue(i, out var s) ? s.Sales : 0m)
                         .ThenBy(i => storeNames.TryGetValue(i, out var n) ? n : string.Empty))
            {
                salesByStore.TryGetValue(id, out var s);
                opExpenses.TryGetValue(id, out var e);
                rows.Add(Make("branch", id,
                    storeNames.TryGetValue(id, out var name) ? name : $"فرع #{id}",
                    s?.Sales ?? 0m, s?.Cost ?? 0m, e?.Amount ?? 0m));
            }

            // مصروفات الإدارة العامة (غير المرتبطة بفرع) تظهر فقط لو فيه مصروف
            if (opExpenses.TryGetValue(NoStore, out var hq) && hq.Amount != 0m)
                rows.Add(Make("hq", null, "الإدارة العامة", 0m, 0m, hq.Amount));

            // الصيانة: تذاكر سُلّمت في اليوم (السعر على العميل - تكلفة القطع)
            var maintBilled = activity.MaintenanceProfitByStore.Sum(r => r.Amount);
            var maintProfit = activity.MaintenanceProfitByStore.Sum(r => r.Profit);
            rows.Add(Make("maintenance", null, "الصيانة", maintBilled, maintBilled - maintProfit));

            // الأجهزة: أجهزة بيعت في اليوم (سعر البيع - سعر الشراء)
            var devSold = activity.DevicesSold.Sum(r => r.Amount);
            var devProfit = activity.DevicesSold.Sum(r => r.Profit);
            rows.Add(Make("devices", null, "الأجهزة", devSold, devSold - devProfit));

            // الرصيد: المستهلك + ربح التقفيل = ما دفعه العملاء، والتكلفة = المستهلك
            var balClosed = activity.BalanceClosures.Sum(r => r.Amount);
            var balProfit = activity.BalanceClosures.Sum(r => r.Profit);
            rows.Add(Make("balance", null, "الرصيد", balClosed + balProfit, balClosed));

            // البنزيمات: بضاعة حُوسبت على التجار في اليوم
            rows.Add(Make("consignments", null, "البنزيمات", activity.ConsignmentRevenue, activity.ConsignmentCost));

            var total = Make("total", null, "الإجمالي",
                rows.Sum(r => r.Revenue), rows.Sum(r => r.Cost), rows.Sum(r => r.Expenses));
            return (rows, total);
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

            dto.OpenCount = open.Count(c => c.OutstandingQuantity > 0 || c.RemainingAmount > 0);
            dto.OpenMoney = open.Where(c => c.RemainingAmount > 0).Sum(c => c.RemainingAmount);
            dto.OpenGoodsValue = open.Where(c => c.OutstandingValue > 0).Sum(c => c.OutstandingValue);

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

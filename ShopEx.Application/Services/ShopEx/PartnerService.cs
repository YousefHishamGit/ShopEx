using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.DTOs.Partners;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class PartnerService : IPartnerService
    {
        private const int MaxPartnersPerBranch = 50;
        private const int MaxNameLength = 150;

        private readonly ISalesRepository _salesRepo;
        private readonly IDashboardRepository _dashboardRepo;
        private readonly IStoreRepository _storeRepo;
        private readonly IPartnerRepository _partnerRepo;
        private readonly IUnitOfWork _unitOfWork;

        public PartnerService(
            ISalesRepository salesRepo,
            IDashboardRepository dashboardRepo,
            IStoreRepository storeRepo,
            IPartnerRepository partnerRepo,
            IUnitOfWork unitOfWork)
        {
            _salesRepo = salesRepo ?? throw new ArgumentNullException(nameof(salesRepo));
            _dashboardRepo = dashboardRepo ?? throw new ArgumentNullException(nameof(dashboardRepo));
            _storeRepo = storeRepo ?? throw new ArgumentNullException(nameof(storeRepo));
            _partnerRepo = partnerRepo ?? throw new ArgumentNullException(nameof(partnerRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ================================================================
        // التقرير
        // ================================================================

        public async Task<PartnersReportResponse> GetReportAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
        {
            var today = DateTime.Now.Date;
            var fromDay = (fromDate ?? new DateTime(today.Year, today.Month, 1)).Date;
            var toDay = (toDate ?? today).Date;
            if (toDay < fromDay)
                throw new ArgumentException("تاريخ النهاية لا يمكن أن يسبق تاريخ البداية.");

            // نهاية الفترة غير شاملة: بداية اليوم التالي لآخر يوم مختار
            var from = fromDay;
            var to = toDay.AddDays(1);

            // تباعاً (وليس بالتوازي): كل DbContext لا يدعم أكثر من استعلام في نفس الوقت.
            var sales = await _salesRepo.GetSalesByStoreAsync(from, to, ct);
            var activity = await _dashboardRepo.GetDailyActivityAsync(from, to, ct);
            var storeNames = await _storeRepo.GetNamesAsync(ct);
            var partners = await _partnerRepo.GetAllAsync(ct);
            var settings = await _partnerRepo.GetSettingsAsync(ct);

            var response = new PartnersReportResponse
            {
                From = fromDay,
                To = toDay,
                Branches = BuildBranches(sales, activity, storeNames, partners, settings)
            };
            (response.Pnl, response.PnlTotal) = DashboardService.BuildPnl(sales, activity, storeNames);
            return response;
        }

        private static List<BranchProfitShareDto> BuildBranches(
            List<StoreSalesRow> sales,
            DailyShopActivity activity,
            Dictionary<int, string> storeNames,
            List<BranchPartner> partners,
            Dictionary<int, BranchPartnerSetting> settings)
        {
            var salesByStore = sales.ToDictionary(s => s.StoreId);
            var maintProfit = DashboardService.ByStore(activity.MaintenanceProfitByStore);
            var devProfit = DashboardService.ByStore(activity.DevicesSold);
            var balProfit = DashboardService.ByStore(activity.BalanceClosures);
            var opExpenses = DashboardService.ByStore(activity.OperatingExpenses);
            var partnersByStore = partners.ToLookup(p => p.StoreId);

            var hq = BranchPartnerSetting.HqStoreId;
            var storeIds = new HashSet<int>(storeNames.Keys);
            storeIds.UnionWith(salesByStore.Keys);
            storeIds.UnionWith(partnersByStore.Select(g => g.Key));
            storeIds.Remove(hq); // الإدارة تُضاف في الآخر كصف مستقل

            // الإدارة العامة = كل ما لا يرتبط بفرع (StoreId = null في الأنشطة) + البنزيمات
            var rows = new List<BranchProfitShareDto>();
            foreach (var id in storeIds.OrderBy(i => i).Append(hq))
            {
                var isHq = id == hq;
                // مفتاح الحركات غير المرتبطة بفرع في القواميس هو DashboardService.NoStore
                var key = isHq ? DashboardService.NoStore : id;

                salesByStore.TryGetValue(id, out var s);
                maintProfit.TryGetValue(key, out var mp);
                devProfit.TryGetValue(key, out var dp);
                balProfit.TryGetValue(key, out var bp);
                opExpenses.TryGetValue(key, out var ex);
                settings.TryGetValue(id, out var setting);

                var row = new BranchProfitShareDto
                {
                    StoreId = id,
                    IsHq = isHq,
                    IsEnabled = !isHq || setting?.IsEnabled != false,
                    StoreName = isHq
                        ? "الإدارة العامة (HQ)"
                        : (storeNames.TryGetValue(id, out var name) ? name : $"فرع #{id}"),
                    SalesProfit = s == null ? 0m : s.Sales - s.Cost,
                    MaintenanceProfit = mp?.Profit ?? 0m,
                    DevicesProfit = dp?.Profit ?? 0m,
                    BalanceProfit = bp?.Profit ?? 0m,
                    ConsignmentProfit = isHq ? activity.ConsignmentRevenue - activity.ConsignmentCost : 0m,
                    Expenses = ex?.Amount ?? 0m,
                    ZakatPercent = setting?.ZakatPercent ?? BranchPartnerSetting.DefaultZakatPercent
                };

                row.TotalProfit = row.SalesProfit + row.MaintenanceProfit + row.DevicesProfit
                                  + row.BalanceProfit + row.ConsignmentProfit - row.Expenses;
                ApplyDistribution(row, partnersByStore[id]);
                rows.Add(row);
            }

            return rows;
        }

        /// <summary>يحسب الزكاة والصافي وحصة كل شريك. لا زكاة على الخسارة، وتُوزَّع الخسارة بنفس النسب.</summary>
        private static void ApplyDistribution(BranchProfitShareDto row, IEnumerable<BranchPartner> partners)
        {
            row.ZakatAmount = row.TotalProfit > 0m ? Money(row.TotalProfit * row.ZakatPercent / 100m) : 0m;
            row.NetAfterZakat = row.TotalProfit - row.ZakatAmount;

            row.Partners = partners.Select(p => new PartnerShareDto
            {
                Id = p.Id,
                Name = p.Name,
                Percent = p.Percent,
                Amount = Money(row.NetAfterZakat * p.Percent / 100m)
            }).ToList();

            row.TotalPercent = row.Partners.Sum(p => p.Percent);
            row.UnallocatedAmount = row.NetAfterZakat - row.Partners.Sum(p => p.Amount);
        }

        // ================================================================
        // تعديل شركاء فرع
        // ================================================================

        public async Task<BranchPartnersConfigDto> SaveBranchPartnersAsync(int storeId, SaveBranchPartnersRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (storeId != BranchPartnerSetting.HqStoreId && !await _storeRepo.ExistsAsync(storeId, ct))
                throw new KeyNotFoundException($"الفرع رقم {storeId} غير موجود.");

            if (request.ZakatPercent < 0m || request.ZakatPercent > 100m)
                throw new ArgumentException("نسبة الزكاة يجب أن تكون بين 0 و 100.");
            var zakat = Math.Round(request.ZakatPercent, 2, MidpointRounding.AwayFromZero);

            var input = request.Partners ?? new List<PartnerInput>();
            if (input.Count > MaxPartnersPerBranch)
                throw new ArgumentException($"أقصى عدد للشركاء في الفرع الواحد {MaxPartnersPerBranch}.");

            var cleaned = new List<(string Name, decimal Percent)>();
            foreach (var p in input)
            {
                var name = (p?.Name ?? string.Empty).Trim();
                if (name.Length == 0)
                    throw new ArgumentException("اسم الشريك مطلوب.");
                if (name.Length > MaxNameLength)
                    throw new ArgumentException($"اسم الشريك '{name}' أطول من {MaxNameLength} حرف.");

                var percent = p!.Percent;
                if (percent <= 0m || percent > 100m)
                    throw new ArgumentException($"نسبة الشريك '{name}' يجب أن تكون أكبر من 0 وحتى 100.");

                cleaned.Add((name, Math.Round(percent, 2, MidpointRounding.AwayFromZero)));
            }

            var totalPercent = cleaned.Sum(c => c.Percent);
            if (totalPercent > 100m)
                throw new ArgumentException($"مجموع نسب الشركاء {totalPercent:0.##}% وهو أكبر من 100%.");

            var existing = await _partnerRepo.GetTrackedByStoreAsync(storeId, ct);
            var setting = await _partnerRepo.GetTrackedSettingAsync(storeId, ct);

            var created = cleaned
                .Select(c => new BranchPartner { StoreId = storeId, Name = c.Name, Percent = c.Percent, CreatedAt = DateTime.Now })
                .ToList();

            _partnerRepo.RemoveRange(existing);
            await _partnerRepo.AddRangeAsync(created, ct);

            if (setting == null)
                await _partnerRepo.AddSettingAsync(new BranchPartnerSetting { StoreId = storeId, ZakatPercent = zakat }, ct);
            else
                setting.ZakatPercent = zakat;

            await _unitOfWork.SaveChangesAsync(ct);

            return new BranchPartnersConfigDto
            {
                StoreId = storeId,
                ZakatPercent = zakat,
                Partners = created.Select(p => new PartnerShareDto { Id = p.Id, Name = p.Name, Percent = p.Percent }).ToList()
            };
        }

        public async Task SetHqEnabledAsync(bool isEnabled, CancellationToken ct = default)
        {
            var hq = BranchPartnerSetting.HqStoreId;
            var setting = await _partnerRepo.GetTrackedSettingAsync(hq, ct);
            if (setting == null)
                await _partnerRepo.AddSettingAsync(new BranchPartnerSetting { StoreId = hq, IsEnabled = isEnabled }, ct);
            else
                setting.IsEnabled = isEnabled;

            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}

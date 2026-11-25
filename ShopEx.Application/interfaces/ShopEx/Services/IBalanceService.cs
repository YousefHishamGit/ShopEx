using ShopEx.Application.DTOs.Balance;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IBalanceService
    {
        /// <summary>
        /// شحن رصيد لفرع معين بمبلغ ووسيلة دفع محددة
        /// </summary>
        Task<BalanceResponse> ChargeAsync(ChargeBalanceRequest request, CancellationToken ct = default);

        /// <summary>
        /// شحن رصيد من الفرع لنفسه: يُنشئ شحنة رصيد للفرع ومصروفاً بنفس المبلغ على الفرع (في معاملة واحدة)
        /// </summary>
        Task<BalanceResponse> ChargeOwnBranchAsync(int storeId, string userName, ChargeOwnBalanceRequest request, CancellationToken ct = default);

        /// <summary>
        /// تقفيل رصيد من الفرع (المبلغ المستخدم فعلياً)
        /// </summary>
        Task<BalanceClosureResponse> CloseAsync(int balanceId, CloseBalanceRequest request, CancellationToken ct = default);

        /// <summary>
        /// استرجاع كل الشحنات مع إمكانية الفلترة بالفرع ونوع الوسيلة والتاريخ
        /// </summary>
        Task<List<BalanceSummaryResponse>> GetAllAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        /// <summary>
        /// استرجاع شحنة رصيد مفصلة بالـ ID مع تقفيلاتها
        /// </summary>
        Task<BalanceResponse?> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// استرجاع تقفيلات شحنة رصيد معينة
        /// </summary>
        Task<List<BalanceClosureResponse>> GetClosuresAsync(int balanceId, CancellationToken ct = default);

        /// <summary>
        /// استرجاع الأرصدة التراكمية مجمعة لكل فرع ولكل وسيلة دفع (تجميع كل الشحنات والتقفيلات في سطر واحد)
        /// </summary>
        Task<List<StoreBalanceGroupDto>> GetGroupedBalancesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            CancellationToken ct = default);

        /// <summary>
        /// تقفيل رصيد يومي على مستوى الفرع ونوع الوسيلة مباشرة وتوزيعه تلقائياً على الشحنات المفتوحة
        /// </summary>
        Task<List<BalanceClosureResponse>> CloseByStoreAsync(
            CloseStoreBalanceRequest request,
            CancellationToken ct = default);

        /// <summary>
        /// استرجاع كشف الحركات والتحويلات التفصيلي (شحنات وتقفيلات بالترتيب الزمني)
        /// </summary>
        Task<List<BalanceTransactionItemDto>> GetLedgerAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);
    }
}

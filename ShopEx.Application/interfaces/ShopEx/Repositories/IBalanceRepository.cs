using ShopEx.Application.DTOs.Balance;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IBalanceRepository
    {
        // ---- Charges (شحن الرصيد من HQ) ----

        /// <summary>
        /// ملخص الشحنات (المستخدم والمتبقي) محسوب داخل قاعدة البيانات بدون تحميل التقفيلات
        /// </summary>
        Task<List<BalanceSummaryResponse>> GetChargeSummariesAsync(
            int? storeId = null,
            BalanceType? balanceType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        /// <summary>
        /// جلب شحنة رصيد واحدة بالـ ID مع تقفيلاتها
        /// </summary>
        Task<Balance?> GetChargeByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// الشحنات التي ما زال بها رصيد متبقي (الأقدم أولاً) لفرع ووسيلة محددين
        /// </summary>
        Task<List<OpenChargeRow>> GetOpenChargesAsync(int storeId, BalanceType balanceType, CancellationToken ct = default);

        /// <summary>
        /// إجمالي ما تم تقفيله من شحنة معينة
        /// </summary>
        Task<decimal> GetUsedAmountAsync(int balanceId, CancellationToken ct = default);

        /// <summary>
        /// إضافة شحنة رصيد جديدة
        /// </summary>
        Task AddChargeAsync(Balance balance, CancellationToken ct = default);

        // ---- Aggregates / Ledger ----

        /// <summary>
        /// الأرصدة المجمعة لكل فرع ووسيلة (تجميع داخل قاعدة البيانات)
        /// </summary>
        Task<List<StoreBalanceGroupDto>> GetGroupedAsync(int? storeId, BalanceType? balanceType, CancellationToken ct = default);

        /// <summary>
        /// صفحة من الحركات (شحن + تقفيل) مرتبة من الأحدث للأقدم، مع إجمالي عدد الحركات المطابقة
        /// </summary>
        Task<(List<BalanceTransactionItemDto> Items, int TotalCount)> GetTransactionsPageAsync(
            int? storeId,
            BalanceType? balanceType,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize,
            CancellationToken ct = default);

        // ---- Closures (تقفيل نهاية اليوم) ----

        /// <summary>
        /// جلب كل تقفيلات رصيد معين
        /// </summary>
        Task<List<BalanceClosure>> GetClosuresByBalanceIdAsync(int balanceId, CancellationToken ct = default);

        /// <summary>
        /// إضافة تقفيل جديد لرصيد معين
        /// </summary>
        Task AddClosureAsync(BalanceClosure closure, CancellationToken ct = default);

        /// <summary>
        /// قفل (داخل المعاملة الحالية) على فرع+وسيلة لمنع تقفيلين متزامنين من تجاوز الرصيد
        /// </summary>
        Task AcquireStoreLockAsync(int storeId, BalanceType balanceType, CancellationToken ct = default);
    }

    /// <summary>
    /// شحنة مفتوحة بالمتبقي منها
    /// </summary>
    public class OpenChargeRow
    {
        public int Id { get; set; }
        public decimal Remaining { get; set; }
    }
}

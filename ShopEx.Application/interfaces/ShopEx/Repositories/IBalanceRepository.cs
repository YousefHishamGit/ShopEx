using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IBalanceRepository
    {
        // ---- Charges (شحن الرصيد من HQ) ----

        /// <summary>
        /// جلب كل شحنات رصيد فرع معين - مع إمكانية فلترة بالنوع والتاريخ
        /// </summary>
        Task<List<Balance>> GetChargesAsync(
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
        /// إضافة شحنة رصيد جديدة
        /// </summary>
        Task AddChargeAsync(Balance balance, CancellationToken ct = default);

        // ---- Closures (تقفيل نهاية اليوم) ----

        /// <summary>
        /// جلب كل تقفيلات رصيد معين
        /// </summary>
        Task<List<BalanceClosure>> GetClosuresByBalanceIdAsync(int balanceId, CancellationToken ct = default);

        /// <summary>
        /// إضافة تقفيل جديد لرصيد معين
        /// </summary>
        Task AddClosureAsync(BalanceClosure closure, CancellationToken ct = default);
    }
}

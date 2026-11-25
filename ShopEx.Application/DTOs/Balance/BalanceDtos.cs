using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Balance
{
    /// <summary>
    /// طلب شحن رصيد لفرع معين من الـ HQ
    /// </summary>
    public class ChargeBalanceRequest
    {
        public int StoreId { get; set; }
        public BalanceType BalanceType { get; set; }
        public decimal Amount { get; set; }
        public DateTime? ChargedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// شحن رصيد من موظف الفرع لفرعه: الفرع والتاريخ يحددهما السيرفر.
    /// </summary>
    public class ChargeOwnBalanceRequest
    {
        public BalanceType BalanceType { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// طلب تقفيل الرصيد اليومي من الفرع (ما استخدمه الفرع فعلياً)
    /// </summary>
    public class CloseBalanceRequest
    {
        public decimal AmountUsed { get; set; }
        /// <summary>الربح المحقق من المبلغ المستهلك (صفر أو أكثر).</summary>
        public decimal Profit { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// بيانات تقفيل الرصيد
    /// </summary>
    public class BalanceClosureResponse
    {
        public int Id { get; set; }
        public int BalanceId { get; set; }
        public decimal AmountUsed { get; set; }
        public decimal Profit { get; set; }
        public DateTime ClosedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// ملخص شحنة الرصيد للقوائم والجداول
    /// </summary>
    public class BalanceSummaryResponse
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public BalanceType BalanceType { get; set; }
        public string BalanceTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ChargedAt { get; set; }
        public string? Notes { get; set; }
        public decimal TotalUsedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public int ClosuresCount { get; set; }
        public bool IsFullySettled => RemainingAmount <= 0;
    }

    /// <summary>
    /// تفاصيل شحنة الرصيد كاملة مع سجل التقفيلات
    /// </summary>
    public class BalanceResponse
    {
        public int Id { get; set; }
        public int StoreId { get; set; }
        public BalanceType BalanceType { get; set; }
        public string BalanceTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ChargedAt { get; set; }
        public string? Notes { get; set; }
        public decimal TotalUsedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsFullySettled => RemainingAmount <= 0;
        public List<BalanceClosureResponse> Closures { get; set; } = new();
    }

    /// <summary>
    /// رصيد الفرع التراكمي المجمع لكل وسيلة دفع (مثال: فرع الهرم - فودافون كاش - إجمالي 2500)
    /// </summary>
    public class StoreBalanceGroupDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public BalanceType BalanceType { get; set; }
        public string BalanceTypeName { get; set; } = string.Empty;
        public decimal TotalCharged { get; set; }
        public decimal TotalUsed { get; set; }
        /// <summary>إجمالي الربح الذي سجله الفرع في تقفيلاته.</summary>
        public decimal TotalProfit { get; set; }
        public decimal CurrentBalance => TotalCharged - TotalUsed;
        public int ChargesCount { get; set; }
        public int ClosuresCount { get; set; }
        public DateTime? LastChargedAt { get; set; }
        public DateTime? LastClosedAt { get; set; }
    }

    /// <summary>
    /// طلب تقفيل رصيد على مستوى الفرع ونوع الوسيلة مباشرة
    /// </summary>
    public class CloseStoreBalanceRequest
    {
        public int StoreId { get; set; }
        public BalanceType BalanceType { get; set; }
        public decimal AmountUsed { get; set; }
        /// <summary>الربح المحقق من المبلغ المستهلك (صفر أو أكثر).</summary>
        public decimal Profit { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>صفحة من كشف الحركات</summary>
    public class BalanceLedgerPageDto
    {
        public List<BalanceTransactionItemDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize)) : 1;
    }

    /// <summary>
    /// عنصر في كشف حركة التحويلات والتقفيلات (Transaction Ledger)
    /// </summary>
    public class BalanceTransactionItemDto
    {
        public int Id { get; set; }
        public string TransactionType { get; set; } = string.Empty; // "Charge" (شحن) أو "Closure" (تقفيل)
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public BalanceType BalanceType { get; set; }
        public string BalanceTypeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>ربح التقفيل (صفر للشحن).</summary>
        public decimal Profit { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public int? BalanceId { get; set; }
    }
}

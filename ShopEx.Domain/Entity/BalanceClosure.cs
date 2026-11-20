namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// تقفيل يومي من الفرع
    /// الفرع بيقول: "اليوم استخدمت X جنيه"
    /// </summary>
    public class BalanceClosure
    {
        public int Id { get; set; }

        /// <summary>
        /// الرصيد المشحون اللي تم التقفيل عليه
        /// </summary>
        public int BalanceId { get; set; }

        /// <summary>
        /// المبلغ الذي استخدمه الفرع فعلياً في هذا التقفيل
        /// </summary>
        public decimal AmountUsed { get; set; }

        /// <summary>
        /// تاريخ التقفيل (يوم التقفيل)
        /// </summary>
        public DateTime ClosedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ملاحظات اختيارية
        /// </summary>
        public string? Notes { get; set; }

        // Navigation
        public Balance Balance { get; set; } = null!;
    }
}

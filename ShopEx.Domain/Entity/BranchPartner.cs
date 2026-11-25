using System;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// شريك في فرع: له نسبة من صافي أرباح الفرع بعد الزكاة.
    /// الفرع (Store) موجود في قاعدة RMS فيُشار إليه بالـ Id فقط (بدون FK).
    /// </summary>
    public class BranchPartner
    {
        public int Id { get; set; }

        public int StoreId { get; set; }

        public string Name { get; set; } = string.Empty;

        /// <summary>نسبة الشريك من صافي الربح بعد الزكاة (0 - 100].</summary>
        public decimal Percent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}

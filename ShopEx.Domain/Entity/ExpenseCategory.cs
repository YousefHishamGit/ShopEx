using System;
using System.Collections.Generic;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// يمثل بند أو تصنيف المصروف (مثل: كهرباء، إيجار، مياه، بوفيه، صيانة مقر، أدوات نظافة، نقل...)
    /// يتم إدخال البنود ديناميكياً وحفظها لتسهيل اختيارها لاحقاً
    /// </summary>
    public class ExpenseCategory
    {
        public int Id { get; set; }

        /// <summary>
        /// اسم بند المصروف (مثال: كهرباء، إيجار، بوفيه، انتقالات)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// وصف إضافي للبند
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// هل البند نشط ومتاح للاختيار
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// تاريخ إضافة البند
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}

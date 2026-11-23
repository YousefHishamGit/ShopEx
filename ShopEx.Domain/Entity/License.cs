using System;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// اشتراك/ترخيص النظام. صف واحد فقط (Id = 1) يتحكم فيه المطوّر يدوياً:
    /// تشغيل/إيقاف + تاريخ انتهاء يتم تمديده (6 شهور / سنة ...).
    /// الاشتراك ساري فقط لو IsEnabled = true و ExpiresAt لم يأتِ بعد.
    /// </summary>
    public class License
    {
        public const int SingletonId = 1;

        public int Id { get; set; } = SingletonId;
        public bool IsEnabled { get; set; } = true;

        /// <summary>تاريخ الانتهاء بتوقيت UTC.</summary>
        public DateTime ExpiresAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>اسم حساب المطوّر الذي أجرى آخر تعديل.</summary>
        public string? UpdatedBy { get; set; }

        /// <summary>لمنع ضياع تعديل لو اتنين ضغطوا في نفس الوقت.</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

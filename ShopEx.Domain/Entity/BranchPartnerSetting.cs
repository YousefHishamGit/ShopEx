namespace ShopEx.Domain.Entity
{
    /// <summary>إعدادات توزيع أرباح فرع (صف واحد لكل فرع يُنشأ عند أول حفظ للشركاء).</summary>
    public class BranchPartnerSetting
    {
        /// <summary>رقم الفرع في RMS (مفتاح أساسي).</summary>
        public int StoreId { get; set; }

        /// <summary>نسبة الزكاة من الأرباح (تُخصم قبل التوزيع على الشركاء).</summary>
        public decimal ZakatPercent { get; set; } = DefaultZakatPercent;

        /// <summary>
        /// تفعيل العرض: يُستخدم للإدارة العامة فقط (يمكن إغلاقها لو لا يوجد شركاء فيها).
        /// الفروع دائماً مفعَّلة.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        public const decimal DefaultZakatPercent = 2.5m;

        /// <summary>معرّف "فرع" الإدارة العامة (HQ) في جداول الشركاء. لا يتعارض مع أي فرع في RMS.</summary>
        public const int HqStoreId = -1;
    }
}

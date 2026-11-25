using ShopEx.Application.DTOs.Dashboard;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Partners
{
    /// <summary>
    /// تقرير الشركاء لفترة: لكل فرع أرباحه وزكاته وحصة كل شريك، + جدول الإيرادات والأرباح للفترة.
    /// </summary>
    public class PartnersReportResponse
    {
        /// <summary>أول يوم في الفترة (شامل).</summary>
        public DateTime From { get; set; }
        /// <summary>آخر يوم في الفترة (شامل).</summary>
        public DateTime To { get; set; }

        public List<BranchProfitShareDto> Branches { get; set; } = new();

        /// <summary>نفس جدول لوحة اليوم (الفروع + الصيانة + الأجهزة + الرصيد + البنزيمات) لكن لفترة.</summary>
        public List<PnlRowDto> Pnl { get; set; } = new();
        public PnlRowDto PnlTotal { get; set; } = new();
    }

    public class BranchProfitShareDto
    {
        /// <summary>رقم الفرع في RMS، أو -1 للإدارة العامة (HQ).</summary>
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;

        /// <summary>الإدارة العامة (تُعامَل كفرع).</summary>
        public bool IsHq { get; set; }
        /// <summary>الفروع دائماً مفعَّلة؛ الإدارة يمكن إغلاقها فتُعرض مطويّة بدون أرقام.</summary>
        public bool IsEnabled { get; set; } = true;

        // ----- مكونات ربح الفرع في الفترة -----
        /// <summary>ربح مبيعات RMS (المبيعات - التكلفة).</summary>
        public decimal SalesProfit { get; set; }
        /// <summary>ربح الصيانة (تذاكر سُلّمت في الفترة).</summary>
        public decimal MaintenanceProfit { get; set; }
        /// <summary>ربح الأجهزة التي باعها الفرع في الفترة.</summary>
        public decimal DevicesProfit { get; set; }
        /// <summary>ربح تقفيل أرصدة الفرع.</summary>
        public decimal BalanceProfit { get; set; }
        /// <summary>ربح البنزيمات (أمانات التجار) - غير مرتبط بفرع فيظهر في الإدارة العامة فقط.</summary>
        public decimal ConsignmentProfit { get; set; }
        /// <summary>مصروفات تشغيلية للفرع (بدون شراء الأجهزة وشحن الرصيد).</summary>
        public decimal Expenses { get; set; }

        /// <summary>مجموع الأرباح = كل ما سبق - المصروفات.</summary>
        public decimal TotalProfit { get; set; }

        public decimal ZakatPercent { get; set; }
        /// <summary>تُحسب فقط عند وجود ربح (لا زكاة على الخسارة).</summary>
        public decimal ZakatAmount { get; set; }
        /// <summary>الصافي بعد الزكاة: هو الذي يُوزَّع على الشركاء.</summary>
        public decimal NetAfterZakat { get; set; }

        public List<PartnerShareDto> Partners { get; set; } = new();
        public decimal TotalPercent { get; set; }
        /// <summary>الجزء غير الموزَّع على أحد لو مجموع النسب أقل من 100%.</summary>
        public decimal UnallocatedAmount { get; set; }
    }

    public class PartnerShareDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Percent { get; set; }
        public decimal Amount { get; set; }
    }

    // ----- تعديل شركاء فرع (Admin) -----

    public class SaveBranchPartnersRequest
    {
        /// <summary>نسبة الزكاة (الافتراضي 2.5).</summary>
        public decimal ZakatPercent { get; set; } = 2.5m;

        /// <summary>القائمة الكاملة للشركاء: تستبدل القائمة الحالية للفرع.</summary>
        public List<PartnerInput> Partners { get; set; } = new();
    }

    public class PartnerInput
    {
        public string Name { get; set; } = string.Empty;
        public decimal Percent { get; set; }
    }

    public class SetEnabledRequest
    {
        public bool IsEnabled { get; set; }
    }

    public class BranchPartnersConfigDto
    {
        public int StoreId { get; set; }
        public decimal ZakatPercent { get; set; }
        public List<PartnerShareDto> Partners { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Maintenance
{
    public class PagedMaintenanceTicketsResponse
    {
        /// <summary>تذاكر الصفحة الحالية فقط.</summary>
        public List<MaintenanceTicketSummaryResponse> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        /// <summary>عدد كل التذاكر المطابقة للفلاتر (وليس الصفحة فقط).</summary>
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize)) : 1;

        // إحصائيات كل التذاكر (بدون فلاتر الحالة والبحث) ضمن نطاق المستخدم
        public int CountTotal { get; set; }
        public int CountReceived { get; set; }
        public int CountCompleted { get; set; }
        public int CountDelivered { get; set; }

        // إجماليات التذاكر المطابقة للفلاتر الحالية (فرع + حالة + بحث). للإدارة فقط: null للموظف.
        /// <summary>تكلفة القطع علينا.</summary>
        public decimal? TotalPartCost { get; set; }
        /// <summary>الربح = السعر على العميل - تكلفة القطع.</summary>
        public decimal? TotalProfit { get; set; }
    }
}

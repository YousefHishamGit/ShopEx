using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Dashboard
{
    /// <summary>لوحة المتابعة اليومية للمدير (HQ).</summary>
    public class DashboardResponse
    {
        /// <summary>اليوم الذي تعرضه اللوحة (المبيعات والأجهزة والمصروفات تخص هذا اليوم).</summary>
        public DateTime Date { get; set; }
        public DateTime GeneratedAt { get; set; }

        public SalesSummaryDto Sales { get; set; } = new();
        public MaintenanceSummaryDto Maintenance { get; set; } = new();
        public ExpensesSummaryDto Expenses { get; set; } = new();
        public BalancesSummaryDto Balances { get; set; } = new();

        /// <summary>فواتير الموردين الآجلة (فترة السماح) - تُحسب دائماً بالنسبة لتاريخ اليوم الفعلي.</summary>
        public InvoiceDuesDto SupplierInvoices { get; set; } = new();

        /// <summary>أمانات التجار (البنزيمات) المتأخرة والقريبة الاستحقاق - بالنسبة لتاريخ اليوم الفعلي.</summary>
        public ConsignmentDuesDto Consignments { get; set; } = new();

        public List<BranchDailyDto> Branches { get; set; } = new();
    }

    public class SalesSummaryDto
    {
        /// <summary>صافي المبيعات (بعد المرتجعات، بدون ضريبة).</summary>
        public decimal TotalSales { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        /// <summary>هامش الربح % من المبيعات.</summary>
        public decimal MarginPercent { get; set; }
        public int TransactionsCount { get; set; }
        public decimal ItemsSold { get; set; }
    }

    public class MaintenanceSummaryDto
    {
        public int ReceivedToday { get; set; }
        public int RepairedToday { get; set; }
        public int DeliveredToday { get; set; }
        /// <summary>المقبوض من العملاء اليوم على الصيانة.</summary>
        public decimal RevenueToday { get; set; }

        /// <summary>أجهزة لم تُسلَّم بعد وما زالت قيد الاستلام/الإصلاح (الآن).</summary>
        public int UnderRepairNow { get; set; }
        /// <summary>أجهزة تم إصلاحها وبانتظار تسليمها للعميل (الآن).</summary>
        public int ReadyForDeliveryNow { get; set; }
    }

    public class ExpensesSummaryDto
    {
        public decimal Total { get; set; }
        public int Count { get; set; }
    }

    public class BalancesSummaryDto
    {
        public decimal ChargedToday { get; set; }
        public decimal ClosedToday { get; set; }
    }

    public class BranchDailyDto
    {
        /// <summary>null = حركات غير مرتبطة بفرع (إدارة عامة / تذاكر قديمة).</summary>
        public int? StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;

        public decimal Sales { get; set; }
        public decimal Profit { get; set; }
        public int TransactionsCount { get; set; }

        public int DevicesReceived { get; set; }
        public int DevicesDelivered { get; set; }

        public decimal Expenses { get; set; }
        public decimal BalanceClosed { get; set; }
    }

    public class InvoiceDuesDto
    {
        /// <summary>عدد الأيام القادمة التي تُعتبر "قريبة الاستحقاق".</summary>
        public int DueSoonDays { get; set; }

        public int OverdueCount { get; set; }
        public decimal OverdueAmount { get; set; }

        public int DueTodayCount { get; set; }
        public decimal DueTodayAmount { get; set; }

        public int DueSoonCount { get; set; }
        public decimal DueSoonAmount { get; set; }

        /// <summary>إجمالي المتبقي على كل الفواتير غير المسددة (متأخرة وغير متأخرة).</summary>
        public decimal TotalOutstanding { get; set; }

        /// <summary>المتأخرة + المستحقة اليوم + القريبة، الأقرب استحقاقاً أولاً.</summary>
        public List<DueInvoiceDto> Items { get; set; } = new();
    }

    public class DueInvoiceDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        /// <summary>سالب = متأخرة بعدد الأيام، 0 = اليوم.</summary>
        public int DaysToDue { get; set; }
    }

    public class ConsignmentDuesDto
    {
        public int DueSoonDays { get; set; }

        public int OverdueCount { get; set; }
        /// <summary>الفلوس المتبقية على التجار في الأمانات المتأخرة.</summary>
        public decimal OverdueMoney { get; set; }
        /// <summary>قيمة البضاعة التي ما زالت مع التجار في الأمانات المتأخرة.</summary>
        public decimal OverdueGoodsValue { get; set; }

        public int DueSoonCount { get; set; }

        public List<DueConsignmentDto> Items { get; set; } = new();
    }

    public class DueConsignmentDto
    {
        public int ConsignmentId { get; set; }
        public string ConsignmentNumber { get; set; } = string.Empty;
        public string DealerName { get; set; } = string.Empty;
        public string DealerPhone { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public decimal RemainingMoney { get; set; }
        public decimal OutstandingGoodsValue { get; set; }
        public int DaysToDue { get; set; }
    }

    // ----- صفوف داخلية بين الـ Repositories والـ Service -----

    /// <summary>مبيعات يوم لفرع واحد من RMS.</summary>
    public class StoreSalesRow
    {
        public int StoreId { get; set; }
        public decimal Sales { get; set; }
        public decimal Cost { get; set; }
        public decimal ItemsSold { get; set; }
        public int TransactionsCount { get; set; }
    }

    /// <summary>قيمة وعدد مجمعين لكل فرع (StoreId = null لغير المرتبط بفرع).</summary>
    public class StoreAmountRow
    {
        public int? StoreId { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class DailyShopActivity
    {
        public List<StoreAmountRow> TicketsReceived { get; set; } = new();
        public List<StoreAmountRow> TicketsDelivered { get; set; } = new();
        public int TicketsRepaired { get; set; }
        public decimal MaintenanceRevenue { get; set; }
        public int UnderRepairNow { get; set; }
        public int ReadyForDeliveryNow { get; set; }

        public List<StoreAmountRow> Expenses { get; set; } = new();
        public List<StoreAmountRow> BalanceClosures { get; set; } = new();
        public decimal BalanceCharged { get; set; }
    }

    public class UnpaidInvoiceRow
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
    }
}

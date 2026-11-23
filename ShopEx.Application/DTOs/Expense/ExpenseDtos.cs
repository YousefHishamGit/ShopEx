using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;

namespace ShopEx.Application.DTOs.Expense
{
    // ================================================================
    // Categories DTOs (بنود المصروفات)
    // ================================================================

    public class ExpenseCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ExpensesCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CreateExpenseCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateExpenseCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ================================================================
    // Expenses DTOs (حركات المصروفات)
    // ================================================================

    public class CreateExpenseRequest
    {
        /// <summary>
        /// اختيار بند موجود مسبقاً بالـ ID
        /// </summary>
        public int? CategoryId { get; set; }

        /// <summary>
        /// أو كتابة اسم بند جديد مباشرة (لو غير موجود، سيتم حفظه تلقائياً في قاعدة البيانات)
        /// </summary>
        public string? CategoryName { get; set; }

        /// <summary>
        /// الفرع التابع له المصروف (اختياري - null يعني إدارة عامة)
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// قيمة المبلغ المصروف
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// تاريخ وتوقيت الصرف (اختياري - افتراضياً الآن)
        /// </summary>
        public DateTime? ExpenseDate { get; set; }

        /// <summary>
        /// طريقة الدفع (كاش، تحويل بنكي، فيزا، محفظة...)
        /// </summary>
        public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

        /// <summary>
        /// الجهة أو الشخص المستلم (مثال: شركة الكهرباء، مالك العقار)
        /// </summary>
        public string? Recipient { get; set; }

        /// <summary>
        /// رقم الإيصال أو الفاتورة
        /// </summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>
        /// ملاحظات
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// اسم المستخدم / الموظف
        /// </summary>
        public string? CreatedBy { get; set; }
    }

    /// <summary>
    /// تسجيل مصروف من موظف الفرع: الفرع واسم الموظف والتاريخ يحددها السيرفر ولا تُقبل من الطلب.
    /// </summary>
    public class CreateBranchExpenseRequest
    {
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal Amount { get; set; }
        public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;
        /// <summary>الجهة/الشخص المستلم (اختياري)</summary>
        public string? Recipient { get; set; }
        /// <summary>تفصيل المصروف: صُرف في إيه (مطلوب)</summary>
        public string? Notes { get; set; }
    }

    public class UpdateExpenseRequest
    {
        /// <summary>
        /// بند موجود بالـ ID، أو اتركه فارغاً واكتب CategoryName لبند جديد
        /// </summary>
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? StoreId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public ExpensePaymentMethod PaymentMethod { get; set; }
        public string? Recipient { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class ExpenseResponse
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public ExpensePaymentMethod PaymentMethod { get; set; }
        public string PaymentMethodName { get; set; } = string.Empty;
        public string? Recipient { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ================================================================
    // Analytics & Summaries (الملخصات والتحليلات)
    // ================================================================

    /// <summary>
    /// فلاتر البحث في المصروفات
    /// </summary>
    public class ExpenseFilter
    {
        public int? StoreId { get; set; }
        public int? CategoryId { get; set; }
        /// <summary>من بداية هذا اليوم (شامل)</summary>
        public DateTime? FromDate { get; set; }
        /// <summary>حتى نهاية هذا اليوم (شامل)</summary>
        public DateTime? ToDate { get; set; }
        public ExpensePaymentMethod? PaymentMethod { get; set; }
        /// <summary>بحث نصي في المستفيد / رقم الإيصال / الملاحظات / اسم البند</summary>
        public string? Search { get; set; }
    }

    public class ExpenseSummaryDto
    {
        /// <summary>إجمالي كل المصروفات المطابقة للفلاتر (وليس الصفحة الحالية فقط)</summary>
        public decimal TotalAmount { get; set; }
        /// <summary>عدد كل المصروفات المطابقة للفلاتر</summary>
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        /// <summary>مصروفات الصفحة الحالية فقط</summary>
        public List<ExpenseResponse> Items { get; set; } = new();
        public List<ExpenseByCategorySummaryDto> CategorySummaries { get; set; } = new();
    }

    public class ExpenseByCategorySummaryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}

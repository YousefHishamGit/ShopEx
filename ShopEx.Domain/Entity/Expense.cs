using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// يمثل حركة المصروف المسجلة
    /// </summary>
    public class Expense
    {
        public int Id { get; set; }

        /// <summary>
        /// معرف بند المصروف التابع له
        /// </summary>
        public int ExpenseCategoryId { get; set; }

        /// <summary>
        /// معرف الفرع المرتبط بالمصروف (اختياري - null يعني مصروف للإدارة العامة / HQ)
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// قيمة المبلغ المصروف
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// تاريخ وتوقيت المصروف
        /// </summary>
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// طريقة الدفع (كاش، تحويل بنكي، فيزا، محفظة...)
        /// </summary>
        public ExpensePaymentMethod PaymentMethod { get; set; } = ExpensePaymentMethod.Cash;

        /// <summary>
        /// الجهة أو الشخص المستلم للمبلغ (مثال: شركة الكهرباء، فني الصيانة، مالك العقار)
        /// </summary>
        public string? Recipient { get; set; }

        /// <summary>
        /// رقم الفاتورة أو إيصال السداد إن وجد
        /// </summary>
        public string? ReceiptNumber { get; set; }

        /// <summary>
        /// ملاحظات وتفاصيل إضافية عن سبب الصرف
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// اسم المستخدم أو الموظف الذي قام بتسجيل المصروف
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// تاريخ تسجيل الحركة في النظام
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ExpenseCategory ExpenseCategory { get; set; } = null!;
    }
}

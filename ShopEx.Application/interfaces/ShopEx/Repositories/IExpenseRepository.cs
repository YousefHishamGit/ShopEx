using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IExpenseRepository
    {
        // ================================================================
        // 1. بنود المصروفات (Expense Categories)
        // ================================================================

        /// <summary>
        /// استرجاع كل بنود المصروفات
        /// </summary>
        Task<List<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);

        /// <summary>
        /// استرجاع بند بالمعرف
        /// </summary>
        Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// استرجاع بند بالاسم (للتحقق من وجوده قبل الإضافة)
        /// </summary>
        Task<ExpenseCategory?> GetCategoryByNameAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// إضافة بند مصروف جديد
        /// </summary>
        Task AddCategoryAsync(ExpenseCategory category, CancellationToken ct = default);

        /// <summary>
        /// تعديل بند مصروف
        /// </summary>
        void UpdateCategory(ExpenseCategory category);

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        /// <summary>
        /// استرجاع قائمة المصروفات مع الفلاتر المتقدمة (الفرع، البند، التاريخ، طريقة الدفع)
        /// </summary>
        Task<List<Expense>> GetExpensesAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default);

        /// <summary>
        /// استرجاع حركة مصروف واحدة بالـ ID مع بيانات البند
        /// </summary>
        Task<Expense?> GetExpenseByIdAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// إضافة حركة مصروف جديدة
        /// </summary>
        Task AddExpenseAsync(Expense expense, CancellationToken ct = default);

        /// <summary>
        /// تعديل حركة مصروف
        /// </summary>
        void UpdateExpense(Expense expense);

        /// <summary>
        /// حذف حركة مصروف
        /// </summary>
        void DeleteExpense(Expense expense);

        /// <summary>
        /// حساب إجمالي المبالغ المصروفة وفقاً للفلاتر
        /// </summary>
        Task<decimal> GetTotalExpensesAmountAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);
    }
}

using ShopEx.Application.DTOs.Expense;
using ShopEx.Domain.Entity;
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

        Task<List<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);

        /// <summary>
        /// عدد وإجمالي المصروفات لكل بند (تجميع داخل قاعدة البيانات)
        /// </summary>
        Task<Dictionary<int, (int Count, decimal Total)>> GetCategoryTotalsAsync(CancellationToken ct = default);

        /// <summary>استرجاع بند للتعديل (tracked)</summary>
        Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default);

        /// <summary>استرجاع بند بالاسم (غير حساس لحالة الأحرف)</summary>
        Task<ExpenseCategory?> GetCategoryByNameAsync(string name, CancellationToken ct = default);

        Task AddCategoryAsync(ExpenseCategory category, CancellationToken ct = default);

        void UpdateCategory(ExpenseCategory category);

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        /// <summary>كل المصروفات المطابقة (بدون ترقيم صفحات)</summary>
        Task<List<Expense>> GetExpensesAsync(ExpenseFilter filter, CancellationToken ct = default);

        /// <summary>صفحة واحدة من المصروفات مرتبة من الأحدث للأقدم</summary>
        Task<List<Expense>> GetExpensesPageAsync(ExpenseFilter filter, int skip, int take, CancellationToken ct = default);

        /// <summary>إجمالي المبلغ وعدد المصروفات المطابقة</summary>
        Task<(decimal Total, int Count)> GetTotalsAsync(ExpenseFilter filter, CancellationToken ct = default);

        /// <summary>التوزيع حسب البند (مجمع داخل قاعدة البيانات)</summary>
        Task<List<ExpenseByCategorySummaryDto>> GetCategoryBreakdownAsync(ExpenseFilter filter, CancellationToken ct = default);

        /// <summary>استرجاع مصروف بالـ ID للتعديل (tracked)</summary>
        Task<Expense?> GetExpenseByIdAsync(int id, CancellationToken ct = default);

        /// <summary>المصروف التلقائي المرتبط بجهاز (tracked) إن وُجد</summary>
        Task<Expense?> GetExpenseByDeviceIdAsync(int deviceId, CancellationToken ct = default);

        Task AddExpenseAsync(Expense expense, CancellationToken ct = default);

        void UpdateExpense(Expense expense);

        void DeleteExpense(Expense expense);
    }
}

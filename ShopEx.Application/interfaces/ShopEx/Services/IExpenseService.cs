using ShopEx.Application.DTOs.Expense;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IExpenseService
    {
        // ================================================================
        // 1. بنود المصروفات (Categories)
        // ================================================================

        Task<List<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default);
        Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default);
        Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken ct = default);
        Task<bool> ToggleCategoryStatusAsync(int id, CancellationToken ct = default);

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default);

        /// <summary>مصروف تلقائي مرتبط بجهاز أو بشحنة رصيد (لا يُعدَّل ولا يُحذف يدوياً)</summary>
        Task<ExpenseResponse> CreateLinkedExpenseAsync(CreateExpenseRequest request, int? deviceId, int? balanceId, CancellationToken ct = default);

        /// <summary>إنشاء/تحديث مصروف شراء الجهاز ليطابق بيانات الجهاز الحالية</summary>
        Task SyncDeviceExpenseAsync(int deviceId, int? storeId, decimal amount, DateTime date, string? recipient, string notes, string? createdBy, CancellationToken ct = default);

        /// <summary>حذف مصروف شراء الجهاز (عند حذف الجهاز)</summary>
        Task DeleteDeviceExpenseAsync(int deviceId, CancellationToken ct = default);
        Task<ExpenseResponse> UpdateExpenseAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default);
        Task<bool> DeleteExpenseAsync(int id, CancellationToken ct = default);
        Task<ExpenseResponse?> GetExpenseByIdAsync(int id, CancellationToken ct = default);

        Task<List<ExpenseResponse>> GetExpensesAsync(ExpenseFilter filter, CancellationToken ct = default);

        /// <summary>
        /// ملخص المصروفات: الإجماليات والتوزيع على البنود محسوبة على كل النتائج، والعناصر (Items) صفحة واحدة فقط
        /// </summary>
        Task<ExpenseSummaryDto> GetExpensesSummaryAsync(ExpenseFilter filter, int page = 1, int pageSize = 25, CancellationToken ct = default);
    }
}

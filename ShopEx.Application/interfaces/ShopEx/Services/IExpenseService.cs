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
        Task<ExpenseResponse> UpdateExpenseAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default);
        Task<bool> DeleteExpenseAsync(int id, CancellationToken ct = default);
        Task<ExpenseResponse?> GetExpenseByIdAsync(int id, CancellationToken ct = default);

        Task<List<ExpenseResponse>> GetExpensesAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default);

        Task<ExpenseSummaryDto> GetExpensesSummaryAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default);
    }
}

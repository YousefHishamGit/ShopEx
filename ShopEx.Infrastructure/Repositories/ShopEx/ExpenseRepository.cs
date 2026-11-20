using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly ShopContext _db;

        public ExpenseRepository(ShopContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // ================================================================
        // 1. بنود المصروفات (Expense Categories)
        // ================================================================

        public Task<List<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var query = _db.ExpenseCategories.AsNoTracking();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return query.OrderBy(c => c.Name).ToListAsync(ct);
        }

        public Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public Task<ExpenseCategory?> GetCategoryByNameAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult<ExpenseCategory?>(null);

            var trimmedName = name.Trim();
            return _db.ExpenseCategories
                      .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName.ToLower(), ct);
        }

        public async Task AddCategoryAsync(ExpenseCategory category, CancellationToken ct = default)
        {
            await _db.ExpenseCategories.AddAsync(category, ct);
        }

        public void UpdateCategory(ExpenseCategory category)
        {
            _db.ExpenseCategories.Update(category);
        }

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        public Task<List<Expense>> GetExpensesAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default)
        {
            var query = _db.Expenses
                           .Include(e => e.ExpenseCategory)
                           .AsNoTracking()
                           .AsQueryable();

            if (storeId.HasValue)
            {
                query = query.Where(e => e.StoreId == storeId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);
            }

            if (paymentMethod.HasValue)
            {
                query = query.Where(e => e.PaymentMethod == paymentMethod.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate <= toDate.Value);
            }

            return query.OrderByDescending(e => e.ExpenseDate).ToListAsync(ct);
        }

        public Task<Expense?> GetExpenseByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Expenses
                      .Include(e => e.ExpenseCategory)
                      .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task AddExpenseAsync(Expense expense, CancellationToken ct = default)
        {
            await _db.Expenses.AddAsync(expense, ct);
        }

        public void UpdateExpense(Expense expense)
        {
            _db.Expenses.Update(expense);
        }

        public void DeleteExpense(Expense expense)
        {
            _db.Expenses.Remove(expense);
        }

        public Task<decimal> GetTotalExpensesAmountAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default)
        {
            var query = _db.Expenses.AsNoTracking().AsQueryable();

            if (storeId.HasValue)
            {
                query = query.Where(e => e.StoreId == storeId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.ExpenseCategoryId == categoryId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.ExpenseDate <= toDate.Value);
            }

            return query.SumAsync(e => e.Amount, ct);
        }
    }
}

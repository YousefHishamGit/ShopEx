using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Expense;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
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
        // 1. بنود المصروفات
        // ================================================================

        public Task<List<ExpenseCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var query = _db.ExpenseCategories.AsNoTracking();

            if (!includeInactive)
                query = query.Where(c => c.IsActive);

            return query.OrderBy(c => c.Name).ToListAsync(ct);
        }

        public async Task<Dictionary<int, (int Count, decimal Total)>> GetCategoryTotalsAsync(CancellationToken ct = default)
        {
            var rows = await _db.Expenses
                .AsNoTracking()
                .GroupBy(e => e.ExpenseCategoryId)
                .Select(g => new { Id = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToListAsync(ct);

            return rows.ToDictionary(r => r.Id, r => (r.Count, r.Total));
        }

        public Task<ExpenseCategory?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public Task<ExpenseCategory?> GetCategoryByNameAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult<ExpenseCategory?>(null);

            // الـ collation الافتراضي في SQL Server غير حساس لحالة الأحرف، والمقارنة المباشرة تستفيد من الـ unique index
            var trimmedName = name.Trim();
            return _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Name == trimmedName, ct);
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
        // 2. حركات المصروفات
        // ================================================================

        private IQueryable<Expense> Filtered(ExpenseFilter f)
        {
            var query = _db.Expenses.AsNoTracking().AsQueryable();

            if (f.StoreId.HasValue)
                query = query.Where(e => e.StoreId == f.StoreId.Value);

            if (f.CategoryId.HasValue)
                query = query.Where(e => e.ExpenseCategoryId == f.CategoryId.Value);

            if (f.PaymentMethod.HasValue)
                query = query.Where(e => e.PaymentMethod == f.PaymentMethod.Value);

            // الفلترة بالأيام الكاملة: من بداية يوم "من" وحتى نهاية يوم "إلى" (شاملاً)
            if (f.FromDate.HasValue)
            {
                var from = f.FromDate.Value.Date;
                query = query.Where(e => e.ExpenseDate >= from);
            }

            if (f.ToDate.HasValue)
            {
                var toExclusive = f.ToDate.Value.Date.AddDays(1);
                query = query.Where(e => e.ExpenseDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                query = query.Where(e =>
                    (e.Recipient != null && e.Recipient.Contains(s)) ||
                    (e.ReceiptNumber != null && e.ReceiptNumber.Contains(s)) ||
                    (e.Notes != null && e.Notes.Contains(s)) ||
                    e.ExpenseCategory.Name.Contains(s));
            }

            return query;
        }

        public Task<List<Expense>> GetExpensesAsync(ExpenseFilter filter, CancellationToken ct = default)
        {
            return Filtered(filter)
                .Include(e => e.ExpenseCategory)
                .OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id)
                .ToListAsync(ct);
        }

        public Task<List<Expense>> GetExpensesPageAsync(ExpenseFilter filter, int skip, int take, CancellationToken ct = default)
        {
            return Filtered(filter)
                .Include(e => e.ExpenseCategory)
                .OrderByDescending(e => e.ExpenseDate).ThenByDescending(e => e.Id)
                .Skip(skip).Take(take)
                .ToListAsync(ct);
        }

        public async Task<(decimal Total, int Count)> GetTotalsAsync(ExpenseFilter filter, CancellationToken ct = default)
        {
            var row = await Filtered(filter)
                .GroupBy(_ => 1)
                .Select(g => new { Total = g.Sum(x => x.Amount), Count = g.Count() })
                .FirstOrDefaultAsync(ct);

            return row == null ? (0m, 0) : (row.Total, row.Count);
        }

        public Task<List<ExpenseByCategorySummaryDto>> GetCategoryBreakdownAsync(ExpenseFilter filter, CancellationToken ct = default)
        {
            return Filtered(filter)
                .GroupBy(e => new { e.ExpenseCategoryId, e.ExpenseCategory.Name })
                .Select(g => new ExpenseByCategorySummaryDto
                {
                    CategoryId = g.Key.ExpenseCategoryId,
                    CategoryName = g.Key.Name,
                    TotalAmount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToListAsync(ct);
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
    }
}

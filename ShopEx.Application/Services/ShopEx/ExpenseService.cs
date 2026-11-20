using ShopEx.Application.DTOs.Expense;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepo;
        private readonly IUnitOfWork _unitOfWork;

        public ExpenseService(
            IExpenseRepository expenseRepo,
            IUnitOfWork unitOfWork)
        {
            _expenseRepo = expenseRepo ?? throw new ArgumentNullException(nameof(expenseRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        // ================================================================
        // 1. بنود المصروفات (Categories)
        // ================================================================

        public async Task<List<ExpenseCategoryDto>> GetCategoriesAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            var categories = await _expenseRepo.GetCategoriesAsync(includeInactive, ct);
            var expenses = await _expenseRepo.GetExpensesAsync(null, null, null, null, null, ct);

            return categories.Select(c =>
            {
                var categoryExpenses = expenses.Where(e => e.ExpenseCategoryId == c.Id).ToList();
                return new ExpenseCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    ExpensesCount = categoryExpenses.Count,
                    TotalAmount = categoryExpenses.Sum(e => e.Amount)
                };
            }).ToList();
        }

        public async Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("يجب إدخال اسم بند المصروف.", nameof(request.Name));

            var trimmedName = request.Name.Trim();
            var existing = await _expenseRepo.GetCategoryByNameAsync(trimmedName, ct);
            if (existing != null)
                throw new InvalidOperationException($"بند المصروف '{trimmedName}' موجود بالفعل.");

            var category = new ExpenseCategory
            {
                Name = trimmedName,
                Description = request.Description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _expenseRepo.AddCategoryAsync(category, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                ExpensesCount = 0,
                TotalAmount = 0
            };
        }

        public async Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("يجب إدخال اسم بند المصروف.", nameof(request.Name));

            var category = await _expenseRepo.GetCategoryByIdAsync(id, ct);
            if (category == null)
                throw new KeyNotFoundException($"بند المصروف برقم {id} غير موجود.");

            var trimmedName = request.Name.Trim();
            var existing = await _expenseRepo.GetCategoryByNameAsync(trimmedName, ct);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException($"اسم البند '{trimmedName}' مستخدم بالفعل لبند آخر.");

            category.Name = trimmedName;
            category.Description = request.Description?.Trim();
            category.IsActive = request.IsActive;

            _expenseRepo.UpdateCategory(category);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ExpenseCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };
        }

        public async Task<bool> ToggleCategoryStatusAsync(int id, CancellationToken ct = default)
        {
            var category = await _expenseRepo.GetCategoryByIdAsync(id, ct);
            if (category == null)
                throw new KeyNotFoundException($"بند المصروف برقم {id} غير موجود.");

            category.IsActive = !category.IsActive;
            _expenseRepo.UpdateCategory(category);
            await _unitOfWork.SaveChangesAsync(ct);

            return category.IsActive;
        }

        // ================================================================
        // 2. حركات المصروفات (Expenses)
        // ================================================================

        public async Task<ExpenseResponse> CreateExpenseAsync(CreateExpenseRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("يجب أن يكون مبلغ المصروف أكبر من الصفر.", nameof(request.Amount));

            int categoryId;
            string categoryName;

            // 1. تحديد البند: إما بالـ ID أو بالاسم (ديناميكياً)
            if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
            {
                var existingCategory = await _expenseRepo.GetCategoryByIdAsync(request.CategoryId.Value, ct);
                if (existingCategory == null)
                    throw new KeyNotFoundException($"بند المصروف برقم {request.CategoryId} غير موجود.");

                categoryId = existingCategory.Id;
                categoryName = existingCategory.Name;
            }
            else if (!string.IsNullOrWhiteSpace(request.CategoryName))
            {
                var trimmed = request.CategoryName.Trim();
                var existingCategory = await _expenseRepo.GetCategoryByNameAsync(trimmed, ct);

                if (existingCategory != null)
                {
                    categoryId = existingCategory.Id;
                    categoryName = existingCategory.Name;
                }
                else
                {
                    // حفظ البند الجديد تلقائياً في قاعدة البيانات ليظهر لاحقاً
                    var newCategory = new ExpenseCategory
                    {
                        Name = trimmed,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _expenseRepo.AddCategoryAsync(newCategory, ct);
                    await _unitOfWork.SaveChangesAsync(ct);

                    categoryId = newCategory.Id;
                    categoryName = newCategory.Name;
                }
            }
            else
            {
                throw new ArgumentException("يجب اختيار بند مصروف أو كتابة اسم بند جديد.");
            }

            // 2. إنشاء حركة المصروف
            var expense = new Expense
            {
                ExpenseCategoryId = categoryId,
                StoreId = request.StoreId,
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate ?? DateTime.UtcNow,
                PaymentMethod = request.PaymentMethod,
                Recipient = request.Recipient?.Trim(),
                ReceiptNumber = request.ReceiptNumber?.Trim(),
                Notes = request.Notes?.Trim(),
                CreatedBy = request.CreatedBy?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _expenseRepo.AddExpenseAsync(expense, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ExpenseResponse
            {
                Id = expense.Id,
                CategoryId = categoryId,
                CategoryName = categoryName,
                StoreId = expense.StoreId,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                PaymentMethod = expense.PaymentMethod,
                PaymentMethodName = GetPaymentMethodName(expense.PaymentMethod),
                Recipient = expense.Recipient,
                ReceiptNumber = expense.ReceiptNumber,
                Notes = expense.Notes,
                CreatedBy = expense.CreatedBy,
                CreatedAt = expense.CreatedAt
            };
        }

        public async Task<ExpenseResponse> UpdateExpenseAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.Amount <= 0)
                throw new ArgumentException("يجب أن يكون مبلغ المصروف أكبر من الصفر.", nameof(request.Amount));

            var expense = await _expenseRepo.GetExpenseByIdAsync(id, ct);
            if (expense == null)
                throw new KeyNotFoundException($"المصروف برقم {id} غير موجود.");

            var category = await _expenseRepo.GetCategoryByIdAsync(request.CategoryId, ct);
            if (category == null)
                throw new KeyNotFoundException($"بند المصروف برقم {request.CategoryId} غير موجود.");

            expense.ExpenseCategoryId = request.CategoryId;
            expense.StoreId = request.StoreId;
            expense.Amount = request.Amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.PaymentMethod = request.PaymentMethod;
            expense.Recipient = request.Recipient?.Trim();
            expense.ReceiptNumber = request.ReceiptNumber?.Trim();
            expense.Notes = request.Notes?.Trim();

            _expenseRepo.UpdateExpense(expense);
            await _unitOfWork.SaveChangesAsync(ct);

            return new ExpenseResponse
            {
                Id = expense.Id,
                CategoryId = category.Id,
                CategoryName = category.Name,
                StoreId = expense.StoreId,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                PaymentMethod = expense.PaymentMethod,
                PaymentMethodName = GetPaymentMethodName(expense.PaymentMethod),
                Recipient = expense.Recipient,
                ReceiptNumber = expense.ReceiptNumber,
                Notes = expense.Notes,
                CreatedBy = expense.CreatedBy,
                CreatedAt = expense.CreatedAt
            };
        }

        public async Task<bool> DeleteExpenseAsync(int id, CancellationToken ct = default)
        {
            var expense = await _expenseRepo.GetExpenseByIdAsync(id, ct);
            if (expense == null)
                return false;

            _expenseRepo.DeleteExpense(expense);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }

        public async Task<ExpenseResponse?> GetExpenseByIdAsync(int id, CancellationToken ct = default)
        {
            var expense = await _expenseRepo.GetExpenseByIdAsync(id, ct);
            if (expense == null)
                return null;

            return MapToResponse(expense);
        }

        public async Task<List<ExpenseResponse>> GetExpensesAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default)
        {
            var expenses = await _expenseRepo.GetExpensesAsync(storeId, categoryId, fromDate, toDate, paymentMethod, ct);
            return expenses.Select(MapToResponse).ToList();
        }

        public async Task<ExpenseSummaryDto> GetExpensesSummaryAsync(
            int? storeId = null,
            int? categoryId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            ExpensePaymentMethod? paymentMethod = null,
            CancellationToken ct = default)
        {
            var expenses = await _expenseRepo.GetExpensesAsync(storeId, categoryId, fromDate, toDate, paymentMethod, ct);
            var items = expenses.Select(MapToResponse).ToList();
            var totalAmount = items.Sum(e => e.Amount);

            var categorySummaries = items
                .GroupBy(e => new { e.CategoryId, e.CategoryName })
                .Select(g =>
                {
                    var sum = g.Sum(e => e.Amount);
                    return new ExpenseByCategorySummaryDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        TotalAmount = sum,
                        Count = g.Count(),
                        Percentage = totalAmount > 0 ? Math.Round((sum / totalAmount) * 100, 2) : 0
                    };
                })
                .OrderByDescending(c => c.TotalAmount)
                .ToList();

            return new ExpenseSummaryDto
            {
                TotalAmount = totalAmount,
                TotalCount = items.Count,
                Items = items,
                CategorySummaries = categorySummaries
            };
        }

        // ================================================================
        // Helper
        // ================================================================

        private static ExpenseResponse MapToResponse(Expense e)
        {
            return new ExpenseResponse
            {
                Id = e.Id,
                CategoryId = e.ExpenseCategoryId,
                CategoryName = e.ExpenseCategory?.Name ?? string.Empty,
                StoreId = e.StoreId,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                PaymentMethod = e.PaymentMethod,
                PaymentMethodName = GetPaymentMethodName(e.PaymentMethod),
                Recipient = e.Recipient,
                ReceiptNumber = e.ReceiptNumber,
                Notes = e.Notes,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt
            };
        }

        private static string GetPaymentMethodName(ExpensePaymentMethod method) => method switch
        {
            ExpensePaymentMethod.Cash => "نقدي (كاش)",
            ExpensePaymentMethod.BankTransfer => "تحويل بنكي / انستاباي",
            ExpensePaymentMethod.CreditCard => "فيزا / كارت",
            ExpensePaymentMethod.ElectronicWallet => "محفظة إلكترونية",
            ExpensePaymentMethod.Cheque => "شيك",
            _ => method.ToString()
        };
    }
}

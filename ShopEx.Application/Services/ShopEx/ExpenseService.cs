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
        private const int MaxCategoryNameLength = 150;
        private const int MaxPageSize = 100;
        private const decimal MaxAmount = 999_999_999m;

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
            var totals = await _expenseRepo.GetCategoryTotalsAsync(ct);

            return categories.Select(c =>
            {
                totals.TryGetValue(c.Id, out var t);
                return MapCategory(c, t.Count, t.Total);
            }).ToList();
        }

        public async Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var name = NormalizeCategoryName(request.Name);

            var existing = await _expenseRepo.GetCategoryByNameAsync(name, ct);
            if (existing != null)
                throw new InvalidOperationException($"بند المصروف '{name}' موجود بالفعل.");

            var category = new ExpenseCategory
            {
                Name = name,
                Description = Clean(request.Description, 500),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            await _expenseRepo.AddCategoryAsync(category, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapCategory(category, 0, 0);
        }

        public async Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var name = NormalizeCategoryName(request.Name);

            var category = await _expenseRepo.GetCategoryByIdAsync(id, ct);
            if (category == null)
                throw new KeyNotFoundException($"بند المصروف برقم {id} غير موجود.");

            var existing = await _expenseRepo.GetCategoryByNameAsync(name, ct);
            if (existing != null && existing.Id != id)
                throw new InvalidOperationException($"اسم البند '{name}' مستخدم بالفعل لبند آخر.");

            category.Name = name;
            category.Description = Clean(request.Description, 500);
            category.IsActive = request.IsActive;

            await _unitOfWork.SaveChangesAsync(ct);

            var totals = await _expenseRepo.GetCategoryTotalsAsync(ct);
            totals.TryGetValue(id, out var t);
            return MapCategory(category, t.Count, t.Total);
        }

        public async Task<bool> ToggleCategoryStatusAsync(int id, CancellationToken ct = default)
        {
            var category = await _expenseRepo.GetCategoryByIdAsync(id, ct);
            if (category == null)
                throw new KeyNotFoundException($"بند المصروف برقم {id} غير موجود.");

            category.IsActive = !category.IsActive;
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

            var amount = ValidateAmount(request.Amount);
            var category = await ResolveCategoryAsync(request.CategoryId, request.CategoryName, currentCategoryId: null, ct);

            var expense = new Expense
            {
                // الربط عبر الـ navigation: لو البند جديد يُحفظ هو والمصروف في عملية حفظ واحدة (atomic)
                ExpenseCategory = category,
                StoreId = NormalizeStoreId(request.StoreId),
                Amount = amount,
                ExpenseDate = request.ExpenseDate ?? DateTime.Now,
                PaymentMethod = ValidatePaymentMethod(request.PaymentMethod),
                Recipient = Clean(request.Recipient, 200),
                ReceiptNumber = Clean(request.ReceiptNumber, 100),
                Notes = Clean(request.Notes, 1000),
                CreatedBy = Clean(request.CreatedBy, 100),
                CreatedAt = DateTime.Now
            };

            await _expenseRepo.AddExpenseAsync(expense, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(expense);
        }

        public async Task<ExpenseResponse> UpdateExpenseAsync(int id, UpdateExpenseRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var amount = ValidateAmount(request.Amount);

            var expense = await _expenseRepo.GetExpenseByIdAsync(id, ct);
            if (expense == null)
                throw new KeyNotFoundException($"المصروف برقم {id} غير موجود.");

            var category = await ResolveCategoryAsync(request.CategoryId, request.CategoryName, expense.ExpenseCategoryId, ct);

            expense.ExpenseCategory = category;
            expense.StoreId = NormalizeStoreId(request.StoreId);
            expense.Amount = amount;
            expense.ExpenseDate = request.ExpenseDate;
            expense.PaymentMethod = ValidatePaymentMethod(request.PaymentMethod);
            expense.Recipient = Clean(request.Recipient, 200);
            expense.ReceiptNumber = Clean(request.ReceiptNumber, 100);
            expense.Notes = Clean(request.Notes, 1000);

            await _unitOfWork.SaveChangesAsync(ct);

            return MapToResponse(expense);
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
            return expense == null ? null : MapToResponse(expense);
        }

        public async Task<List<ExpenseResponse>> GetExpensesAsync(ExpenseFilter filter, CancellationToken ct = default)
        {
            var expenses = await _expenseRepo.GetExpensesAsync(filter ?? new ExpenseFilter(), ct);
            return expenses.Select(MapToResponse).ToList();
        }

        public async Task<ExpenseSummaryDto> GetExpensesSummaryAsync(
            ExpenseFilter filter, int page = 1, int pageSize = 25, CancellationToken ct = default)
        {
            filter ??= new ExpenseFilter();
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // الإجماليات والتوزيع على البنود بتجميع في قاعدة البيانات، والصفحة الحالية فقط تُحمّل بالكامل
            // (الاستعلامات تتم تتابعياً لأنها تشترك في نفس الـ DbContext)
            var (total, count) = await _expenseRepo.GetTotalsAsync(filter, ct);
            var breakdown = await _expenseRepo.GetCategoryBreakdownAsync(filter, ct);

            var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var rows = await _expenseRepo.GetExpensesPageAsync(filter, (page - 1) * pageSize, pageSize, ct);

            foreach (var c in breakdown)
                c.Percentage = total > 0 ? Math.Round(c.TotalAmount / total * 100, 2) : 0;

            return new ExpenseSummaryDto
            {
                TotalAmount = total,
                TotalCount = count,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = rows.Select(MapToResponse).ToList(),
                CategorySummaries = breakdown
            };
        }

        // ================================================================
        // Helpers
        // ================================================================

        /// <summary>
        /// تحديد البند: بالـ ID أو بالاسم (ينشئ بنداً جديداً في الذاكرة إن لم يكن موجوداً، ويُحفظ مع المصروف).
        /// البنود المعطلة لا يمكن اختيارها لمصروفات جديدة (إلا إذا كان المصروف المعدَّل يستخدمها أصلاً).
        /// </summary>
        private async Task<ExpenseCategory> ResolveCategoryAsync(
            int? categoryId, string? categoryName, int? currentCategoryId, CancellationToken ct)
        {
            ExpenseCategory? category;

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                category = await _expenseRepo.GetCategoryByIdAsync(categoryId.Value, ct);
                if (category == null)
                    throw new KeyNotFoundException($"بند المصروف برقم {categoryId} غير موجود.");
            }
            else if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var name = NormalizeCategoryName(categoryName);
                category = await _expenseRepo.GetCategoryByNameAsync(name, ct);

                if (category == null)
                {
                    category = new ExpenseCategory
                    {
                        Name = name,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    await _expenseRepo.AddCategoryAsync(category, ct);
                    return category;
                }
            }
            else
            {
                throw new ArgumentException("يجب اختيار بند مصروف أو كتابة اسم بند جديد.");
            }

            if (!category.IsActive && category.Id != currentCategoryId)
                throw new InvalidOperationException($"بند المصروف '{category.Name}' معطل. فعّله أولاً أو اختر بنداً آخر.");

            return category;
        }

        private static decimal ValidateAmount(decimal amount)
        {
            var rounded = Math.Round(amount, 2);
            if (rounded <= 0)
                throw new ArgumentException("يجب أن يكون مبلغ المصروف أكبر من الصفر.", nameof(amount));
            if (rounded > MaxAmount)
                throw new ArgumentException("مبلغ المصروف أكبر من الحد المسموح.", nameof(amount));
            return rounded;
        }

        private static ExpensePaymentMethod ValidatePaymentMethod(ExpensePaymentMethod method)
        {
            if (!Enum.IsDefined(typeof(ExpensePaymentMethod), method))
                throw new ArgumentException("طريقة الدفع غير صحيحة.", nameof(method));
            return method;
        }

        private static int? NormalizeStoreId(int? storeId) => storeId is > 0 ? storeId : null;

        private static string NormalizeCategoryName(string? name)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new ArgumentException("يجب إدخال اسم بند المصروف.", nameof(name));
            if (trimmed.Length > MaxCategoryNameLength)
                throw new ArgumentException($"اسم البند يجب ألا يتجاوز {MaxCategoryNameLength} حرفاً.", nameof(name));
            return trimmed;
        }

        private static string? Clean(string? value, int maxLength)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }

        private static ExpenseCategoryDto MapCategory(ExpenseCategory c, int count, decimal total) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt,
            ExpensesCount = count,
            TotalAmount = total
        };

        private static ExpenseResponse MapToResponse(Expense e) => new()
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

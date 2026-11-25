using Microsoft.EntityFrameworkCore;
using ShopEx.Application.Services.ShopEx;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb
{
    public static class ShopDbSeeder
    {
        public static async Task SeedAsync(ShopContext context, CancellationToken ct = default)
        {
            if (!await context.Warehouses.AnyAsync(ct))
            {
                var hqWarehouse = new Warehouse
                {
                    Name = "HQ Warehouse",
                    Location = "Main Office",
                    IsDefault = true,
                    IsActive = true
                };

                await context.Warehouses.AddAsync(hqWarehouse, ct);
                await context.SaveChangesAsync(ct);
            }

            await BackfillDevicePurchaseExpensesAsync(context, ct);
        }

        /// <summary>
        /// أجهزة أُدخلت قبل ربط الشراء بالمصروفات: يُنشأ لها مصروف الشراء مرة واحدة (آمن للتكرار).
        /// </summary>
        private static async Task BackfillDevicePurchaseExpensesAsync(ShopContext context, CancellationToken ct)
        {
            var missing = await context.Devices.AsNoTracking()
                .Where(d => !context.Expenses.Any(e => e.DeviceId == d.Id))
                .ToListAsync(ct);
            if (missing.Count == 0) return;

            var category = await context.ExpenseCategories
                .FirstOrDefaultAsync(c => c.Name == ExpenseService.DeviceExpenseCategory, ct);
            if (category == null)
            {
                category = new ExpenseCategory
                {
                    Name = ExpenseService.DeviceExpenseCategory,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.ExpenseCategories.Add(category);
            }

            foreach (var d in missing)
            {
                context.Expenses.Add(new Expense
                {
                    ExpenseCategory = category,
                    StoreId = d.StoreId,
                    Amount = d.PurchasePrice,
                    ExpenseDate = d.PurchaseDate,
                    PaymentMethod = ExpensePaymentMethod.Cash,
                    Recipient = d.SellerName,
                    Notes = $"شراء جهاز {d.Brand} {d.Model}".Trim() + (string.IsNullOrEmpty(d.Imei) ? "" : $" (IMEI {d.Imei})"),
                    CreatedBy = d.CreatedBy,
                    CreatedAt = DateTime.Now,
                    DeviceId = d.Id
                });
            }

            await context.SaveChangesAsync(ct);
        }
    }
}

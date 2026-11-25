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

            await RemoveDevicePurchaseExpensesAsync(context, ct);
        }

        /// <summary>
        /// شراء الجهاز وشحن رصيد الفرع لم يعودا مصروفاً (مثل فواتير الموردين): يحذف المصروفات التلقائية
        /// القديمة المرتبطة بهما، ثم بندَي "شراء أجهزة" و"شحن رصيد" لو أصبحا فارغين. آمن للتكرار.
        /// </summary>
        private static async Task RemoveDevicePurchaseExpensesAsync(ShopContext context, CancellationToken ct)
        {
            await context.Expenses
                .Where(e => e.DeviceId != null || e.BalanceId != null)
                .ExecuteDeleteAsync(ct);

            var legacyCategories = new[] { ExpenseService.DeviceExpenseCategory, BalanceService.SelfChargeExpenseCategory };
            await context.ExpenseCategories
                .Where(c => legacyCategories.Contains(c.Name)
                            && !context.Expenses.Any(e => e.ExpenseCategoryId == c.Id))
                .ExecuteDeleteAsync(ct);
        }
    }
}

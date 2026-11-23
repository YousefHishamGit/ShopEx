using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb
{
    public static class DevSeeder
    {
        /// <summary>
        /// يُنشئ حساب المطوّر الأول من الإعدادات (InitialDev:Username / InitialDev:Password) لو جدول DevUsers فاضي.
        /// لا يوجد حساب افتراضي: لو الإعدادات غير موجودة لا يُنشأ شيء.
        /// </summary>
        public static async Task SeedInitialDevAsync(
            ShopContext context,
            IPasswordHasher hasher,
            string? username,
            string? password,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (await context.DevUsers.AnyAsync(ct))
                return;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                logger.LogWarning("لا يوجد حساب مطوّر. اضبط InitialDev:Username و InitialDev:Password (User Secrets أو متغيرات البيئة) ثم أعد التشغيل.");
                return;
            }

            if (password.Length < 8)
                throw new InvalidOperationException("InitialDev:Password يجب ألا تقل عن 8 أحرف.");

            context.DevUsers.Add(new DevUser
            {
                Name = "Developer",
                Username = username.Trim(),
                PasswordHash = hasher.Hash(password),
                IsActive = true,
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(ct);
            logger.LogInformation("تم إنشاء حساب المطوّر '{Username}'. يُنصح بحذف InitialDev:Password من الإعدادات الآن.", username.Trim());
        }

        /// <summary>
        /// يضمن وجود صف الاشتراك. أول مرة: فترة سماح (افتراضياً 30 يوم) تكفي المطوّر ليضبط الاشتراك الفعلي.
        /// </summary>
        public static async Task EnsureLicenseAsync(ShopContext context, int graceDays, CancellationToken ct = default)
        {
            if (await context.Licenses.AnyAsync(ct))
                return;

            var now = DateTime.UtcNow;
            context.Licenses.Add(new License
            {
                Id = License.SingletonId,
                IsEnabled = true,
                ExpiresAt = now.AddDays(Math.Max(graceDays, 1)),
                UpdatedAt = now,
                UpdatedBy = "system"
            });
            await context.SaveChangesAsync(ct);
        }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Licensing
{
    public static class LicenseMessages
    {
        public const string Blocked = "الاشتراك منتهي أو متوقف. تواصل مع المطوّر لتجديده.";

        /// <summary>كود ثابت يقرأه الـ Frontend للتفريق بين منع الاشتراك ومنع الصلاحيات.</summary>
        public const string BlockedCode = "LICENSE_EXPIRED";
    }

    /// <summary>الحالة المخزنة مؤقتاً للتحقق في كل طلب (الصلاحية تُحسب وقت الفحص من التاريخ).</summary>
    public record LicenseState(bool IsEnabled, DateTime ExpiresAt)
    {
        public bool IsValid(DateTime utcNow) => IsEnabled && ExpiresAt > utcNow;
    }

    public class LicenseResponse
    {
        public bool IsEnabled { get; set; }
        public DateTime ExpiresAt { get; set; }

        /// <summary>ساري فعلاً (مفعّل ولم ينتهِ).</summary>
        public bool IsValid { get; set; }

        /// <summary>Active | Disabled | Expired</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>الأيام المتبقية (0 لو منتهي).</summary>
        public int DaysRemaining { get; set; }

        public DateTime UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class SetLicenseEnabledRequest
    {
        public bool IsEnabled { get; set; }
    }

    public class ExtendLicenseRequest
    {
        [Range(1, 60, ErrorMessage = "عدد الشهور من 1 إلى 60.")]
        public int Months { get; set; }
    }

    public class DevLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
    }
}

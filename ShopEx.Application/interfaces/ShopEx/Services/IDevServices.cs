using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    /// <summary>تسجيل دخول المطوّر (جدول DevUsers المنفصل).</summary>
    public interface IDevAuthService
    {
        Task<DevLoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    }

    public interface ILicenseService
    {
        Task<LicenseResponse> GetAsync(CancellationToken ct = default);

        /// <summary>هل الاشتراك ساري الآن؟ (يستخدم تخزيناً مؤقتاً قصيراً).</summary>
        Task<bool> IsValidAsync(CancellationToken ct = default);

        Task<LicenseResponse> SetEnabledAsync(bool isEnabled, string updatedBy, CancellationToken ct = default);

        /// <summary>
        /// يزيد المدة بعدد شهور: من تاريخ الانتهاء الحالي لو لسه ساري، أو من اليوم لو منتهي.
        /// </summary>
        Task<LicenseResponse> ExtendAsync(int months, string updatedBy, CancellationToken ct = default);
    }
}

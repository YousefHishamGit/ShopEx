using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IDevUserRepository
    {
        Task<DevUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

        /// <summary>قراءة خفيفة للتحقق من التوكن.</summary>
        Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken ct = default);
    }

    public interface ILicenseRepository
    {
        /// <summary>النسخة المتتبَّعة للتعديل.</summary>
        Task<License?> GetAsync(CancellationToken ct = default);

        /// <summary>قراءة خفيفة للفحص في كل طلب.</summary>
        Task<LicenseState?> GetStateAsync(CancellationToken ct = default);
    }
}

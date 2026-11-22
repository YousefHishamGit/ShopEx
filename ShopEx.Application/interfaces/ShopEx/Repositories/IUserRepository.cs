using ShopEx.Application.DTOs.Users;
using ShopEx.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

        /// <summary>عدد الـ Admins الفعالين (مع استبعاد مستخدم معين).</summary>
        Task<int> CountActiveAdminsAsync(int? excludeUserId, CancellationToken ct = default);

        /// <summary>قراءة خفيفة (عمودين فقط) للتحقق من التوكن.</summary>
        Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken ct = default);

        Task AddAsync(User user, CancellationToken ct = default);
        void Remove(User user);
    }
}

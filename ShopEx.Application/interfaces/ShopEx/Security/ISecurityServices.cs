using ShopEx.Application.DTOs.Users;
using ShopEx.Domain.Entity;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Security
{
    /// <summary>إصدار JWT يحتوي على: الهوية، الدور، والفرع (storeId).</summary>
    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) CreateToken(User user);
    }

    /// <summary>
    /// حالة المستخدم المستخدمة للتحقق من التوكن في كل طلب (فعال؟ والـ SecurityStamp)،
    /// مع تخزين مؤقت قصير حتى لا يُضرب الـ DB في كل Request.
    /// </summary>
    public interface IUserAuthStateProvider
    {
        Task<UserAuthState?> GetAsync(int userId, CancellationToken ct = default);

        /// <summary>يجب استدعاؤها عند تغيير كلمة المرور أو تعطيل/حذف المستخدم ليسري المنع فوراً.</summary>
        void Invalidate(int userId);
    }

    /// <summary>الحماية من تخمين كلمات المرور (قفل مؤقت بعد محاولات فاشلة متكررة).</summary>
    public interface ILoginAttemptTracker
    {
        bool IsLocked(string username);
        void RecordFailure(string username);
        void Reset(string username);
    }
}

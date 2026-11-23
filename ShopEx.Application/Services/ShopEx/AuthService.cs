using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.Exceptions;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class AuthService : IAuthService
    {
        // رسالة موحدة حتى لا نكشف هل اسم المستخدم موجود أم لا
        private const string InvalidCredentialsMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";

        private readonly IUserRepository _userRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ILoginAttemptTracker _attempts;
        private readonly IStoreRepository _storeRepo;
        private readonly ILicenseService _license;

        public AuthService(
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            ILoginAttemptTracker attempts,
            IStoreRepository storeRepo,
            ILicenseService license)
        {
            _license = license;
            _userRepo = userRepo;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _attempts = attempts;
            _storeRepo = storeRepo;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var username = (request.Username ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            if (username.Length == 0 || password.Length == 0)
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);

            var lockKey = username.ToLowerInvariant();
            if (_attempts.IsLocked(lockKey))
                throw new AccountLockedException("تم إيقاف تسجيل الدخول مؤقتاً بسبب محاولات فاشلة كثيرة. حاول مرة أخرى بعد بضع دقائق.");

            var user = await _userRepo.GetByUsernameAsync(username, ct);

            // نقارن دائماً (حتى لو المستخدم غير موجود) لتقليل الفروق الزمنية التي تكشف وجود الحساب
            var passwordOk = PasswordMatches(user?.Password ?? string.Empty, password);
            if (user == null || !passwordOk)
            {
                _attempts.RecordFailure(lockKey);
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);
            }

            if (!user.IsActive)
                throw new UnauthorizedAccessException("هذا الحساب معطل. تواصل مع المدير.");

            _attempts.Reset(lockKey);

            // الاشتراك منتهي/متوقف: لا دخول لأي مستخدم عادي (المطوّر له تسجيل دخول منفصل)
            if (!await _license.IsValidAsync(ct))
                throw new UnauthorizedAccessException(LicenseMessages.Blocked);

            user.LastLoginAt = DateTime.Now;
            await _unitOfWork.SaveChangesAsync(ct);

            var (token, expiresAt) = _tokenService.CreateToken(user);

            string? storeName = null;
            if (user.StoreId.HasValue)
            {
                var names = await _storeRepo.GetNamesAsync(ct);
                names.TryGetValue(user.StoreId.Value, out storeName);
            }

            return new LoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = UserService.Map(user, storeName)
            };
        }

        /// <summary>
        /// مقارنة ثابتة الزمن. كلمات المرور محفوظة بدون تشفير بناءً على طلب صاحب النظام؛
        /// لو تقرر التشفير لاحقاً يتم تغيير هذه الدالة (وأماكن حفظ كلمة المرور في UserService) فقط.
        /// </summary>
        private static bool PasswordMatches(string stored, string provided)
        {
            var a = Encoding.UTF8.GetBytes(stored);
            var b = Encoding.UTF8.GetBytes(provided);
            return a.Length == b.Length && a.Length > 0 && CryptographicOperations.FixedTimeEquals(a, b);
        }
    }
}

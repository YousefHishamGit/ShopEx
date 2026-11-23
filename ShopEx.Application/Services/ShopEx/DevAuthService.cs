using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.Exceptions;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class DevAuthService : IDevAuthService
    {
        private const string InvalidCredentialsMessage = "اسم المستخدم أو كلمة المرور غير صحيحة.";

        private readonly IDevUserRepository _devRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly ILoginAttemptTracker _attempts;
        private readonly IPasswordHasher _hasher;

        // يُستخدم للمقارنة لو المستخدم غير موجود، حتى لا يختلف زمن الرد ويكشف وجود الحساب
        private static string? _dummyHash;

        public DevAuthService(
            IDevUserRepository devRepo,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            ILoginAttemptTracker attempts,
            IPasswordHasher hasher)
        {
            _devRepo = devRepo;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _attempts = attempts;
            _hasher = hasher;
        }

        public async Task<DevLoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var username = (request.Username ?? string.Empty).Trim();
            var password = request.Password ?? string.Empty;
            if (username.Length == 0 || password.Length == 0)
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);

            // مفتاح مستقل عن مستخدمي النظام العاديين
            var lockKey = "dev:" + username.ToLowerInvariant();
            if (_attempts.IsLocked(lockKey))
                throw new AccountLockedException("تم إيقاف تسجيل الدخول مؤقتاً بسبب محاولات فاشلة كثيرة. حاول مرة أخرى بعد بضع دقائق.");

            var dev = await _devRepo.GetByUsernameAsync(username, ct);

            _dummyHash ??= _hasher.Hash(Guid.NewGuid().ToString("N"));
            var passwordOk = _hasher.Verify(dev?.PasswordHash ?? _dummyHash, password);
            if (dev == null || !passwordOk)
            {
                _attempts.RecordFailure(lockKey);
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);
            }

            if (!dev.IsActive)
                throw new UnauthorizedAccessException(InvalidCredentialsMessage);

            _attempts.Reset(lockKey);

            dev.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);

            var (token, expiresAt) = _tokenService.CreateDevToken(dev);

            return new DevLoginResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Id = dev.Id,
                Name = dev.Name,
                Username = dev.Username
            };
        }
    }
}

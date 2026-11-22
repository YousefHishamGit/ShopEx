using ShopEx.Application.DTOs.Users;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class UserService : IUserService
    {
        private static readonly Regex UsernamePattern = new(@"^[A-Za-z0-9._-]{3,50}$", RegexOptions.Compiled);
        private static readonly Regex PhonePattern = new(@"^[0-9+\-\s()]{7,30}$", RegexOptions.Compiled);

        private readonly IUserRepository _userRepo;
        private readonly IStoreRepository _storeRepo;
        private readonly IUserAuthStateProvider _authState;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IUserRepository userRepo,
            IStoreRepository storeRepo,
            IUserAuthStateProvider authState,
            IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _storeRepo = storeRepo;
            _authState = authState;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var name = request.Name?.Trim() ?? string.Empty;
            var phone = request.Phone?.Trim() ?? string.Empty;
            var username = request.Username?.Trim() ?? string.Empty;

            if (name.Length == 0)
                throw new ArgumentException("الاسم مطلوب.");
            if (!PhonePattern.IsMatch(phone))
                throw new ArgumentException("رقم الهاتف غير صحيح.");
            if (!UsernamePattern.IsMatch(username))
                throw new ArgumentException("اسم المستخدم يجب أن يكون من 3 إلى 50 حرف (حروف إنجليزية وأرقام و . _ - فقط).");
            if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6 || request.Password.Length > 100)
                throw new ArgumentException("كلمة المرور يجب أن تكون من 6 إلى 100 حرف.");
            if (!Enum.IsDefined(typeof(UserRole), request.Role))
                throw new ArgumentException("الدور غير صحيح.");

            if (request.Role == UserRole.Employee && !request.StoreId.HasValue)
                throw new ArgumentException("يجب تحديد الفرع للموظف.");

            string? storeName = null;
            if (request.StoreId.HasValue)
            {
                var stores = await _storeRepo.GetNamesAsync(ct);
                if (!stores.TryGetValue(request.StoreId.Value, out storeName))
                    throw new ArgumentException("الفرع المحدد غير موجود.");
            }

            if (await _userRepo.UsernameExistsAsync(username, ct))
                throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل.");

            var user = new User
            {
                Name = name,
                Phone = phone,
                Role = request.Role,
                Username = username,
                Password = request.Password,
                StoreId = request.StoreId,
                IsActive = true,
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = DateTime.Now
            };

            await _userRepo.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Map(user, storeName);
        }

        public async Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default)
        {
            var users = await _userRepo.GetAllAsync(ct);
            var stores = users.Any(u => u.StoreId.HasValue)
                ? await _storeRepo.GetNamesAsync(ct)
                : new Dictionary<int, string>();

            return users
                .Select(u => Map(u, u.StoreId.HasValue && stores.TryGetValue(u.StoreId.Value, out var n) ? n : null))
                .ToList();
        }

        public async Task UpdatePasswordAsync(int userId, UpdatePasswordRequest request, CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6 || request.NewPassword.Length > 100)
                throw new ArgumentException("كلمة المرور يجب أن تكون من 6 إلى 100 حرف.");

            var user = await _userRepo.GetByIdAsync(userId, ct)
                       ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            user.Password = request.NewPassword;
            // تغيير الـ Stamp يُبطل كل الجلسات (التوكنات) الحالية لهذا المستخدم
            user.SecurityStamp = Guid.NewGuid();

            await _unitOfWork.SaveChangesAsync(ct);
            _authState.Invalidate(userId);
        }

        public async Task<UserResponse> SetActiveAsync(int userId, bool isActive, int currentUserId, CancellationToken ct = default)
        {
            var user = await _userRepo.GetByIdAsync(userId, ct)
                       ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            if (!isActive)
                await EnsureCanRemoveAsync(user, currentUserId, "تعطيل", ct);

            user.IsActive = isActive;
            user.SecurityStamp = Guid.NewGuid();

            await _unitOfWork.SaveChangesAsync(ct);
            _authState.Invalidate(userId);

            string? storeName = null;
            if (user.StoreId.HasValue)
                (await _storeRepo.GetNamesAsync(ct)).TryGetValue(user.StoreId.Value, out storeName);

            return Map(user, storeName);
        }

        public async Task DeleteAsync(int userId, int currentUserId, CancellationToken ct = default)
        {
            var user = await _userRepo.GetByIdAsync(userId, ct)
                       ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            await EnsureCanRemoveAsync(user, currentUserId, "حذف", ct);

            _userRepo.Remove(user);
            await _unitOfWork.SaveChangesAsync(ct);
            _authState.Invalidate(userId);
        }

        /// <summary>يمنع الـ Admin من تعطيل/حذف نفسه، ويمنع ترك النظام بدون أي Admin فعال.</summary>
        private async Task EnsureCanRemoveAsync(User user, int currentUserId, string action, CancellationToken ct)
        {
            if (user.Id == currentUserId)
                throw new InvalidOperationException($"لا يمكنك {action} حسابك الحالي.");

            if (user.Role == UserRole.Admin && user.IsActive &&
                await _userRepo.CountActiveAdminsAsync(user.Id, ct) == 0)
            {
                throw new InvalidOperationException($"لا يمكن {action} آخر مدير فعال في النظام.");
            }
        }

        internal static UserResponse Map(User u, string? storeName) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Phone = u.Phone,
            Role = u.Role,
            Username = u.Username,
            StoreId = u.StoreId,
            StoreName = storeName,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        };
    }
}

using ShopEx.Domain.Enum;
using System;
using System.ComponentModel.DataAnnotations;

namespace ShopEx.Application.DTOs.Users
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [StringLength(50, ErrorMessage = "اسم المستخدم غير صحيح.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [StringLength(100, ErrorMessage = "كلمة المرور غير صحيحة.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = new();
    }

    public class CreateUserRequest
    {
        [Required(ErrorMessage = "الاسم مطلوب.")]
        [StringLength(150, ErrorMessage = "الاسم لا يتعدى 150 حرف.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب.")]
        [StringLength(30, ErrorMessage = "رقم الهاتف لا يتعدى 30 حرف.")]
        public string Phone { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Employee;

        [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "اسم المستخدم من 3 إلى 50 حرف.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور من 6 إلى 100 حرف.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>إجباري للموظف (Employee)، واختياري للـ Admin.</summary>
        public int? StoreId { get; set; }
    }

    public class UpdatePasswordRequest
    {
        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمة المرور من 6 إلى 100 حرف.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>بيانات المستخدم للعرض. لا تحتوي على كلمة المرور أبداً.</summary>
    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string RoleName => Role.ToString();
        public string Username { get; set; } = string.Empty;
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>الحالة المستخدمة للتحقق من التوكن في كل طلب (يتم تخزينها مؤقتاً).</summary>
    public record UserAuthState(bool IsActive, Guid SecurityStamp);
}

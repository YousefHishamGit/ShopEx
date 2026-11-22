using ShopEx.Domain.Enum;
using System;

namespace ShopEx.Domain.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Employee;
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// كلمة المرور محفوظة كما هي (بدون تشفير) بناءً على طلب صاحب النظام.
        /// لا تُرجع هذه القيمة في أي Response أبداً.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// الفرع (معرّف الفرع في RMS). إجباري للموظف، واختياري للـ Admin (HQ).
        /// </summary>
        public int? StoreId { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// يتغير عند تغيير كلمة المرور أو تعطيل المستخدم، فيُبطل كل التوكنات الصادرة قبل ذلك.
        /// </summary>
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
    }
}

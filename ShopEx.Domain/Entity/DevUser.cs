using System;

namespace ShopEx.Domain.Entity
{
    /// <summary>
    /// حساب المطوّر (Role = Dev). جدول منفصل تماماً عن Users حتى لا يظهر للعميل في أي شاشة أو API،
    /// وكلمة المرور هنا مخزنة Hash (PBKDF2) وليست نصاً صريحاً.
    /// </summary>
    public class DevUser
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        /// <summary>Hash بصيغة: PBKDF2-SHA256$iterations$salt$hash. لا تُرجع هذه القيمة في أي Response.</summary>
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        /// <summary>يتغير عند تغيير كلمة المرور أو التعطيل فيُبطل التوكنات القديمة.</summary>
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}

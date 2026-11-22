using System;

namespace ShopEx.Application.Exceptions
{
    /// <summary>يُرمى عند تجاوز عدد محاولات تسجيل الدخول الفاشلة.</summary>
    public class AccountLockedException : Exception
    {
        public AccountLockedException(string message) : base(message) { }
    }
}

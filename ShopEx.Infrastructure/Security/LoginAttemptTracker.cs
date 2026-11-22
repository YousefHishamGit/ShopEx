using Microsoft.Extensions.Caching.Memory;
using ShopEx.Application.interfaces.ShopEx.Security;
using System;

namespace ShopEx.Infrastructure.Security
{
    /// <summary>
    /// قفل مؤقت لاسم المستخدم بعد عدة محاولات دخول فاشلة متتالية (في الذاكرة).
    /// </summary>
    public class LoginAttemptTracker : ILoginAttemptTracker
    {
        public const int MaxFailures = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(5);
        private static readonly object Gate = new();

        private readonly IMemoryCache _cache;

        public LoginAttemptTracker(IMemoryCache cache)
        {
            _cache = cache;
        }

        public bool IsLocked(string username)
            => _cache.TryGetValue(Key(username), out int failures) && failures >= MaxFailures;

        public void RecordFailure(string username)
        {
            lock (Gate)
            {
                _cache.TryGetValue(Key(username), out int failures);
                // كل محاولة فاشلة تجدد مدة القفل
                _cache.Set(Key(username), failures + 1, LockDuration);
            }
        }

        public void Reset(string username) => _cache.Remove(Key(username));

        private static string Key(string username) => $"login-failures:{username}";
    }
}

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Security
{
    /// <summary>
    /// يتحقق من أن صاحب التوكن ما زال موجوداً وفعالاً وأن الـ SecurityStamp لم يتغير.
    /// النتيجة تُخزن مؤقتاً لفترة قصيرة (Singleton) حتى لا يُضرب الـ DB في كل Request،
    /// ويتم مسحها فوراً عند تغيير كلمة المرور أو تعطيل/حذف المستخدم.
    /// </summary>
    public class CachedUserAuthStateProvider : IUserAuthStateProvider
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
        private static readonly UserAuthState Missing = new(false, Guid.Empty);

        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        public CachedUserAuthStateProvider(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public async Task<UserAuthState?> GetAsync(int userId, CancellationToken ct = default)
        {
            var key = CacheKey(userId);
            if (_cache.TryGetValue(key, out UserAuthState? cached))
                return ReferenceEquals(cached, Missing) ? null : cached;

            // الـ Provider Singleton بينما الـ Repository Scoped، لذلك ننشئ Scope قصير
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var state = await repo.GetAuthStateAsync(userId, ct);

            _cache.Set(key, state ?? Missing, Ttl);
            return state;
        }

        public void Invalidate(int userId) => _cache.Remove(CacheKey(userId));

        private static string CacheKey(int userId) => $"auth-state:{userId}";
    }
}

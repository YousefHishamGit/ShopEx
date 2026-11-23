using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Security
{
    /// <summary>حالة حساب المطوّر للتحقق من التوكن (Singleton + Scope قصير للـ Repository).</summary>
    public class CachedDevAuthStateProvider : IDevAuthStateProvider
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);
        private static readonly UserAuthState Missing = new(false, Guid.Empty);

        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        public CachedDevAuthStateProvider(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public async Task<UserAuthState?> GetAsync(int devId, CancellationToken ct = default)
        {
            var key = CacheKey(devId);
            if (_cache.TryGetValue(key, out UserAuthState? cached))
                return ReferenceEquals(cached, Missing) ? null : cached;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDevUserRepository>();
            var state = await repo.GetAuthStateAsync(devId, ct);

            _cache.Set(key, state ?? Missing, Ttl);
            return state;
        }

        public void Invalidate(int devId) => _cache.Remove(CacheKey(devId));

        private static string CacheKey(int devId) => $"dev-auth-state:{devId}";
    }

    /// <summary>
    /// حالة الاشتراك تُقرأ في كل طلب API، لذلك تُخزن ثواني قليلة فقط.
    /// تخزن القيم الخام (مفعّل + تاريخ الانتهاء) والصلاحية تُحسب وقت الفحص، فالانتهاء يسري في لحظته.
    /// </summary>
    public class CachedLicenseStateProvider : ILicenseStateProvider
    {
        private const string Key = "license-state";
        private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;

        public CachedLicenseStateProvider(IMemoryCache cache, IServiceScopeFactory scopeFactory)
        {
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public async Task<LicenseState?> GetAsync(CancellationToken ct = default)
        {
            if (_cache.TryGetValue(Key, out LicenseState? cached))
                return cached;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<ILicenseRepository>();
            var state = await repo.GetStateAsync(ct);

            // الغياب (null) لا يُخزن حتى لا يتعطل النظام بعد إنشاء الصف
            if (state != null)
                _cache.Set(Key, state, Ttl);
            return state;
        }

        public void Invalidate() => _cache.Remove(Key);
    }
}

using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILicenseStateProvider _state;

        public LicenseService(ILicenseRepository repo, IUnitOfWork unitOfWork, ILicenseStateProvider state)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _state = state;
        }

        public async Task<LicenseResponse> GetAsync(CancellationToken ct = default)
            => Map(await LoadAsync(ct));

        public async Task<bool> IsValidAsync(CancellationToken ct = default)
        {
            var state = await _state.GetAsync(ct);
            // لو الصف غير موجود نمنع (Fail closed)
            return state != null && state.IsValid(DateTime.UtcNow);
        }

        public async Task<LicenseResponse> SetEnabledAsync(bool isEnabled, string updatedBy, CancellationToken ct = default)
        {
            var license = await LoadAsync(ct);
            license.IsEnabled = isEnabled;
            await SaveAsync(license, updatedBy, ct);
            return Map(license);
        }

        public async Task<LicenseResponse> ExtendAsync(int months, string updatedBy, CancellationToken ct = default)
        {
            if (months < 1 || months > 60)
                throw new ArgumentException("عدد الشهور من 1 إلى 60.");

            var license = await LoadAsync(ct);

            var now = DateTime.UtcNow;
            var start = license.ExpiresAt > now ? license.ExpiresAt : now;
            license.ExpiresAt = start.AddMonths(months);

            await SaveAsync(license, updatedBy, ct);
            return Map(license);
        }

        private async Task<License> LoadAsync(CancellationToken ct)
            => await _repo.GetAsync(ct) ?? throw new KeyNotFoundException("بيانات الاشتراك غير موجودة.");

        private async Task SaveAsync(License license, string updatedBy, CancellationToken ct)
        {
            license.UpdatedAt = DateTime.UtcNow;
            license.UpdatedBy = updatedBy;
            await _unitOfWork.SaveChangesAsync(ct);
            _state.Invalidate(); // المنع/السماح يسري فوراً
        }

        private static LicenseResponse Map(License l)
        {
            var now = DateTime.UtcNow;
            var expired = l.ExpiresAt <= now;
            return new LicenseResponse
            {
                IsEnabled = l.IsEnabled,
                ExpiresAt = DateTime.SpecifyKind(l.ExpiresAt, DateTimeKind.Utc),
                IsValid = l.IsEnabled && !expired,
                Status = !l.IsEnabled ? "Disabled" : expired ? "Expired" : "Active",
                DaysRemaining = expired ? 0 : (int)Math.Ceiling((l.ExpiresAt - now).TotalDays),
                UpdatedAt = DateTime.SpecifyKind(l.UpdatedAt, DateTimeKind.Utc),
                UpdatedBy = l.UpdatedBy
            };
        }
    }
}

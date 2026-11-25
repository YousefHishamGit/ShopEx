using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class PartnerRepository : IPartnerRepository
    {
        private readonly ShopContext _db;

        public PartnerRepository(ShopContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task<List<BranchPartner>> GetAllAsync(CancellationToken ct = default)
            => _db.BranchPartners.AsNoTracking()
                .OrderByDescending(p => p.Percent)
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .ToListAsync(ct);

        public async Task<Dictionary<int, BranchPartnerSetting>> GetSettingsAsync(CancellationToken ct = default)
            => (await _db.BranchPartnerSettings.AsNoTracking().ToListAsync(ct))
                .ToDictionary(s => s.StoreId);

        public Task<List<BranchPartner>> GetTrackedByStoreAsync(int storeId, CancellationToken ct = default)
            => _db.BranchPartners.Where(p => p.StoreId == storeId).ToListAsync(ct);

        public Task<BranchPartnerSetting?> GetTrackedSettingAsync(int storeId, CancellationToken ct = default)
            => _db.BranchPartnerSettings.FirstOrDefaultAsync(s => s.StoreId == storeId, ct);

        public void RemoveRange(IEnumerable<BranchPartner> partners)
            => _db.BranchPartners.RemoveRange(partners);

        public Task AddRangeAsync(IEnumerable<BranchPartner> partners, CancellationToken ct = default)
            => _db.BranchPartners.AddRangeAsync(partners, ct);

        public async Task AddSettingAsync(BranchPartnerSetting setting, CancellationToken ct = default)
            => await _db.BranchPartnerSettings.AddAsync(setting, ct);
    }
}

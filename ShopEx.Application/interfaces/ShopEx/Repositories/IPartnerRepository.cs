using ShopEx.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IPartnerRepository
    {
        /// <summary>كل الشركاء (قراءة فقط)، مرتبين بالنسبة تنازلياً ثم بالاسم.</summary>
        Task<List<BranchPartner>> GetAllAsync(CancellationToken ct = default);

        /// <summary>إعدادات الزكاة لكل فرع له إعدادات (قراءة فقط).</summary>
        Task<Dictionary<int, BranchPartnerSetting>> GetSettingsAsync(CancellationToken ct = default);

        /// <summary>شركاء فرع (متتبَّعون للتعديل/الحذف).</summary>
        Task<List<BranchPartner>> GetTrackedByStoreAsync(int storeId, CancellationToken ct = default);

        Task<BranchPartnerSetting?> GetTrackedSettingAsync(int storeId, CancellationToken ct = default);

        void RemoveRange(IEnumerable<BranchPartner> partners);
        Task AddRangeAsync(IEnumerable<BranchPartner> partners, CancellationToken ct = default);
        Task AddSettingAsync(BranchPartnerSetting setting, CancellationToken ct = default);
    }
}

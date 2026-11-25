using ShopEx.Application.DTOs.Partners;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IPartnerService
    {
        /// <param name="fromDate">أول يوم (شامل). الافتراضي: أول الشهر الحالي.</param>
        /// <param name="toDate">آخر يوم (شامل). الافتراضي: اليوم.</param>
        Task<PartnersReportResponse> GetReportAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);

        /// <summary>يستبدل شركاء الفرع ونسبة زكاته بالقائمة المرسلة.</summary>
        Task<BranchPartnersConfigDto> SaveBranchPartnersAsync(int storeId, SaveBranchPartnersRequest request, CancellationToken ct = default);

        /// <summary>فتح/إغلاق كارت الإدارة العامة (HQ) في صفحة الشركاء.</summary>
        Task SetHqEnabledAsync(bool isEnabled, CancellationToken ct = default);
    }
}

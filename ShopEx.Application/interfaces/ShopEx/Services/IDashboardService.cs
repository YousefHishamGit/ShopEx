using ShopEx.Application.DTOs.Dashboard;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IDashboardService
    {
        /// <param name="date">اليوم المطلوب (الافتراضي: اليوم).</param>
        Task<DashboardResponse> GetAsync(DateTime? date = null, CancellationToken ct = default);
    }
}

using ShopEx.Application.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IDashboardRepository
    {
        /// <summary>نشاط الفترة [from, to): الأجهزة، الصيانة، المصروفات، الأرصدة.</summary>
        Task<DailyShopActivity> GetDailyActivityAsync(DateTime from, DateTime to, CancellationToken ct = default);

        /// <summary>فواتير الموردين التي ما زال عليها متبقٍ ولها تاريخ استحقاق.</summary>
        Task<List<UnpaidInvoiceRow>> GetUnpaidDatedInvoicesAsync(CancellationToken ct = default);

        /// <summary>إجمالي المتبقي على كل الفواتير غير المسددة (حتى التي بدون تاريخ استحقاق).</summary>
        Task<decimal> GetTotalOutstandingInvoicesAsync(CancellationToken ct = default);
    }
}

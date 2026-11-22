using ShopEx.Application.DTOs.Dashboard;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface ISalesRepository
    {
        /// <summary>
        /// مبيعات وتكلفة كل فرع في الفترة [from, to) من حركات RMS (صافي بعد المرتجعات).
        /// </summary>
        Task<List<StoreSalesRow>> GetSalesByStoreAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }
}

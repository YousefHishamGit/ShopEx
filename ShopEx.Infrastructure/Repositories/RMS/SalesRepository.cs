using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Infrastructure.Persistence.RMS;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class SalesRepository : ISalesRepository
    {
        /// <summary>ItemType = 9 سطور لا تُحتسب كمبيعات في تقارير RMS (نفس استعلام RMS المرجعي).</summary>
        private const int ExcludedItemType = 9;

        private readonly RMSContext _db;

        public SalesRepository(RMSContext db)
        {
            _db = db;
        }

        public async Task<List<StoreSalesRow>> GetSalesByStoreAsync(DateTime start, DateTime end, CancellationToken ct = default)
        {
            // RMS قاعدة نقاط بيع حية: نقرأ بدون أقفال (مثل WITH(NOLOCK) في تقارير RMS) كي لا نعطّل الكاشير.
            await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, ct);

            // السعر والتكلفة للوحدة؛ الكمية سالبة في المرتجعات فيخرج الصافي مباشرة.
            // الحساب بـ double لأن الكمية float (decimal * float كانت ستُقرّب الكمية الكسرية).
            var lines = await (
                from e in _db.TransactionEntries.AsNoTracking()
                join t in _db.Transactions.AsNoTracking()
                    on new { e.StoreID, e.TransactionNumber } equals new { t.StoreID, t.TransactionNumber }
                where t.Time >= start && t.Time < end && e.ItemType != ExcludedItemType
                group new { e.Price, e.Cost, e.Quantity } by t.StoreID into g
                select new
                {
                    StoreId = g.Key,
                    Sales = g.Sum(x => (double)x.Price * x.Quantity),
                    Cost = g.Sum(x => (double)x.Cost * x.Quantity),
                    Items = g.Sum(x => x.Quantity)
                }).ToListAsync(ct);

            var counts = await _db.Transactions.AsNoTracking()
                .Where(t => t.Time >= start && t.Time < end)
                .GroupBy(t => t.StoreID)
                .Select(g => new { StoreId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            await tx.CommitAsync(ct);

            var byStore = lines.ToDictionary(l => l.StoreId);
            var countByStore = counts.ToDictionary(c => c.StoreId, c => c.Count);

            return byStore.Keys.Union(countByStore.Keys)
                .Select(id =>
                {
                    byStore.TryGetValue(id, out var l);
                    countByStore.TryGetValue(id, out var count);
                    return new StoreSalesRow
                    {
                        StoreId = id,
                        Sales = l == null ? 0m : Money(l.Sales),
                        Cost = l == null ? 0m : Money(l.Cost),
                        ItemsSold = l == null ? 0m : Money(l.Items),
                        TransactionsCount = count
                    };
                })
                .ToList();
        }

        private static decimal Money(double value) => Math.Round((decimal)value, 2);
    }
}

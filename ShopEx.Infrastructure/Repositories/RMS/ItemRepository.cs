using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Domain.RMSEntity;
using ShopEx.Infrastructure.Persistence.RMS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.RMS
{
    public class ItemRepository : IItemRepository
    {
        private readonly RMSContext _context;

        public ItemRepository(RMSContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Item>> GetItemsBySupplierIdAsync(int supplierId)
        {
            return await _context.Items
                .Where(i => i.SupplierID == supplierId)
                .ToListAsync();
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _context.Items.FindAsync(id);
        }

        public async Task<Dictionary<int, Item>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return new Dictionary<int, Item>();
            return await _context.Items.AsNoTracking()
                .Where(i => list.Contains(i.ID))
                .ToDictionaryAsync(i => i.ID);
        }

        public async Task<Dictionary<int, decimal>> GetPricesByIdsAsync(IEnumerable<int> ids)
        {
            var result = new Dictionary<int, decimal>();
            foreach (var chunk in ids.Distinct().Chunk(1000))
            {
                var part = chunk.ToList();
                var rows = await _context.Items.AsNoTracking()
                    .Where(i => part.Contains(i.ID))
                    .Select(i => new { i.ID, i.Price })
                    .ToListAsync();
                foreach (var r in rows)
                    result[r.ID] = r.Price ?? 0m;
            }
            return result;
        }

        public async Task<Item?> GetByLookupCodeAsync(string lookupCode)
        {
            if (string.IsNullOrWhiteSpace(lookupCode)) return null;
            return await _context.Items.FirstOrDefaultAsync(i => i.ItemLookupCode == lookupCode.Trim());
        }

        public Task<List<int>> SearchIdsAsync(string term, int limit = 1000)
        {
            term = term.Trim();
            return _context.Items.AsNoTracking()
                .Where(i => (i.Description != null && i.Description.Contains(term)) ||
                            (i.ItemLookupCode != null && i.ItemLookupCode.Contains(term)) ||
                            i.ID.ToString() == term)
                .Select(i => i.ID)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<Item>> SearchItemsAsync(string? query, int limit = 50)
        {
            var q = _context.Items.AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim();
                q = q.Where(i => (!string.IsNullOrEmpty(i.Description) && i.Description.Contains(term)) ||
                                 (!string.IsNullOrEmpty(i.ItemLookupCode) && i.ItemLookupCode.Contains(term)) ||
                                 i.ID.ToString().Contains(term));
            }
            return await q.OrderBy(i => i.Description).Take(limit).ToListAsync();
        }
    }
}

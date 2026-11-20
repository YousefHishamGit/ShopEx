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
    }
}

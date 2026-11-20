using ShopEx.Domain.RMSEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.RMS.Repositories
{
    public interface IItemRepository
    {
        Task<IEnumerable<Item>> GetItemsBySupplierIdAsync(int supplierId);
    }
}

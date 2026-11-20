using ShopEx.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IWarehouseRepository
    {
        Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default);
        Task<List<Warehouse>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
        Task AddAsync(Warehouse warehouse, CancellationToken ct = default);
        void Update(Warehouse warehouse);
        void Delete(Warehouse warehouse);
    }
}

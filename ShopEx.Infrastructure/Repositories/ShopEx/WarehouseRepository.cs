using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly ShopContext _db;

        public WarehouseRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Warehouses
                      .FirstOrDefaultAsync(w => w.ID == id, ct);
        }

        public Task<Warehouse?> GetDefaultAsync(CancellationToken ct = default)
        {
            return _db.Warehouses
                      .FirstOrDefaultAsync(w => w.IsDefault && w.IsActive, ct);
        }

        public Task<List<Warehouse>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
        {
            IQueryable<Warehouse> query = _db.Warehouses;

            if (activeOnly)
            {
                query = query.Where(w => w.IsActive);
            }

            return query.OrderByDescending(w => w.IsDefault)
                        .ThenBy(w => w.Name)
                        .ToListAsync(ct);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            var query = _db.Warehouses.Where(w => w.Name == name);
            if (excludeId.HasValue)
            {
                query = query.Where(w => w.ID != excludeId.Value);
            }
            return query.AnyAsync(ct);
        }

        public async Task AddAsync(Warehouse warehouse, CancellationToken ct = default)
        {
            await _db.Warehouses.AddAsync(warehouse, ct);
        }

        public void Update(Warehouse warehouse)
        {
            _db.Warehouses.Update(warehouse);
        }

        public void Delete(Warehouse warehouse)
        {
            _db.Warehouses.Remove(warehouse);
        }
    }
}

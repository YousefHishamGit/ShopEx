using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class DealerRepository : IDealerRepository
    {
        private readonly ShopContext _db;

        public DealerRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<Dealer?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.Dealers
                      .Include(d => d.Consignments.Where(c => c.Status == ConsignmentStatus.Open))
                          .ThenInclude(c => c.Items)
                              .ThenInclude(i => i.SettlementItems)
                      .Include(d => d.Consignments.Where(c => c.Status == ConsignmentStatus.Open))
                          .ThenInclude(c => c.Payments)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(d => d.ID == id, ct);
        }

        public Task<List<Dealer>> GetAllAsync(bool? activeOnly = null, string? search = null, CancellationToken ct = default)
        {
            // Open consignments are loaded so the dealer cards can show active count / outstanding / unpaid
            IQueryable<Dealer> query = _db.Dealers
                .Include(d => d.Consignments.Where(c => c.Status == ConsignmentStatus.Open))
                    .ThenInclude(c => c.Items)
                        .ThenInclude(i => i.SettlementItems)
                .Include(d => d.Consignments.Where(c => c.Status == ConsignmentStatus.Open))
                    .ThenInclude(c => c.Payments)
                .AsSplitQuery()
                .AsNoTrackingWithIdentityResolution();

            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(d => d.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(d => d.Name.Contains(search) || d.Phone.Contains(search));
            }

            return query.OrderBy(d => d.Name).ToListAsync(ct);
        }

        public Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken ct = default)
        {
            var query = _db.Dealers.Where(d => d.Phone == phone);
            if (excludeId.HasValue)
            {
                query = query.Where(d => d.ID != excludeId.Value);
            }
            return query.AnyAsync(ct);
        }

        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
        {
            var query = _db.Dealers.Where(d => d.Name == name);
            if (excludeId.HasValue)
            {
                query = query.Where(d => d.ID != excludeId.Value);
            }
            return query.AnyAsync(ct);
        }

        public async Task AddAsync(Dealer dealer, CancellationToken ct = default)
        {
            await _db.Dealers.AddAsync(dealer, ct);
        }

        public void Update(Dealer dealer)
        {
            _db.Dealers.Update(dealer);
        }

        public void Delete(Dealer dealer)
        {
            _db.Dealers.Remove(dealer);
        }
    }
}

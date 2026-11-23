using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class DevUserRepository : IDevUserRepository
    {
        private readonly ShopContext _db;

        public DevUserRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<DevUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => _db.DevUsers.FirstOrDefaultAsync(d => d.Username == username, ct);

        public Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken ct = default)
            => _db.DevUsers.AsNoTracking()
                           .Where(d => d.Id == id)
                           .Select(d => new UserAuthState(d.IsActive, d.SecurityStamp))
                           .FirstOrDefaultAsync(ct);
    }

    public class LicenseRepository : ILicenseRepository
    {
        private readonly ShopContext _db;

        public LicenseRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<License?> GetAsync(CancellationToken ct = default)
            => _db.Licenses.FirstOrDefaultAsync(l => l.Id == License.SingletonId, ct);

        public Task<LicenseState?> GetStateAsync(CancellationToken ct = default)
            => _db.Licenses.AsNoTracking()
                           .Where(l => l.Id == License.SingletonId)
                           .Select(l => new LicenseState(l.IsEnabled, l.ExpiresAt))
                           .FirstOrDefaultAsync(ct);
    }
}

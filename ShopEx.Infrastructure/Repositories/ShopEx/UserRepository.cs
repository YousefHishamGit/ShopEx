using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Users;
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
    public class UserRepository : IUserRepository
    {
        private readonly ShopContext _db;

        public UserRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => _db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

        public Task<List<User>> GetAllAsync(CancellationToken ct = default)
            => _db.Users.AsNoTracking()
                        .OrderBy(u => u.Role)
                        .ThenBy(u => u.Name)
                        .ToListAsync(ct);

        public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
            => _db.Users.AnyAsync(u => u.Username == username, ct);

        public Task<int> CountActiveAdminsAsync(int? excludeUserId, CancellationToken ct = default)
            => _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != excludeUserId, ct);

        public Task<UserAuthState?> GetAuthStateAsync(int id, CancellationToken ct = default)
            => _db.Users.AsNoTracking()
                        .Where(u => u.Id == id)
                        .Select(u => new UserAuthState(u.IsActive, u.SecurityStamp))
                        .FirstOrDefaultAsync(ct);

        public async Task AddAsync(User user, CancellationToken ct = default)
            => await _db.Users.AddAsync(user, ct);

        public void Remove(User user)
            => _db.Users.Remove(user);
    }
}

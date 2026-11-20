using ShopEx.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IDealerRepository
    {
        Task<Dealer?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<List<Dealer>> GetAllAsync(bool? activeOnly = null, string? search = null, CancellationToken ct = default);
        Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);
        Task AddAsync(Dealer dealer, CancellationToken ct = default);
        void Update(Dealer dealer);
        void Delete(Dealer dealer);
    }
}

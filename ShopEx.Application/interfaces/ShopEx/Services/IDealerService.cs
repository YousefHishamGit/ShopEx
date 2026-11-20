using ShopEx.Application.DTOs.Consignment;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IDealerService
    {
        Task<List<DealerResponse>> GetAllAsync(bool? activeOnly = null, string? search = null, CancellationToken ct = default);
        Task<DealerResponse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<DealerResponse> CreateAsync(CreateDealerRequest request, CancellationToken ct = default);
        Task<DealerResponse> UpdateAsync(int id, UpdateDealerRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}

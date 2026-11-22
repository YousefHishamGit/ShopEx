using ShopEx.Application.DTOs.Users;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IUserService
    {
        Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
        Task<List<UserResponse>> GetAllAsync(CancellationToken ct = default);
        Task UpdatePasswordAsync(int userId, UpdatePasswordRequest request, CancellationToken ct = default);
        Task<UserResponse> SetActiveAsync(int userId, bool isActive, int currentUserId, CancellationToken ct = default);
        Task DeleteAsync(int userId, int currentUserId, CancellationToken ct = default);
    }
}

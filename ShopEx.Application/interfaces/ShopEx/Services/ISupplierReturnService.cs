using ShopEx.Application.DTOs.SupplierReturn;
using ShopEx.Domain.Enum;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface ISupplierReturnService
    {
        Task<SupplierReturnResponse> CreateAsync(CreateSupplierReturnRequest request, CancellationToken ct = default);
        Task<SupplierReturnResponse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<SupplierReturnResponse?> GetByReturnNumberAsync(string returnNumber, CancellationToken ct = default);
        Task<List<SupplierReturnSummaryResponse>> GetAllAsync(int? rmsSupplierId = null, SupplierReturnStatus? status = null, int? warehouseId = null, CancellationToken ct = default);
        Task<SupplierReturnResponse> ConfirmReturnAsync(int id, CancellationToken ct = default);
        Task<SupplierReturnResponse> CancelReturnAsync(int id, CancellationToken ct = default);
        Task<SupplierReturnResponse> AddRefundAsync(int returnId, AddSupplierRefundRequest request, CancellationToken ct = default);
    }
}

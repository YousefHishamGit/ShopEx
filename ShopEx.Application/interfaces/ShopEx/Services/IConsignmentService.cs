using ShopEx.Application.DTOs.Consignment;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IConsignmentService
    {
        Task<ConsignmentResponse> CreateAsync(CreateConsignmentRequest request, CancellationToken ct = default);
        Task<ConsignmentResponse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ConsignmentResponse?> GetByNumberAsync(string consignmentNumber, CancellationToken ct = default);
        Task<List<ConsignmentSummaryResponse>> GetAllAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);
        Task<ConsignmentsPageResponse> GetPagedAsync(
            int? dealerId, string? status, string? search, int page, int pageSize, CancellationToken ct = default);
        Task<ConsignmentResponse> AddSettlementAsync(int id, CreateConsignmentSettlementRequest request, CancellationToken ct = default);
        Task<ConsignmentResponse> AddPaymentAsync(int id, AddConsignmentPaymentRequest request, CancellationToken ct = default);
        Task<ConsignmentResponse> CancelConsignmentAsync(int id, CancellationToken ct = default);
    }
}

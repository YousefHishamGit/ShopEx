using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IConsignmentRepository
    {
        Task<Consignment?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Consignment?> GetByConsignmentNumberAsync(string consignmentNumber, CancellationToken ct = default);
        Task<List<Consignment>> GetAllAsync(
            int? dealerId = null,
            ConsignmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            CancellationToken ct = default);

        Task<bool> ExistsAsync(string consignmentNumber, CancellationToken ct = default);
        Task<string> GetNextConsignmentNumberAsync(CancellationToken ct = default);

        Task AddAsync(Consignment consignment, CancellationToken ct = default);
        void Update(Consignment consignment);

        Task AddItemAsync(ConsignmentItem item, CancellationToken ct = default);
        void RemoveItem(ConsignmentItem item);

        Task AddSettlementAsync(ConsignmentSettlement settlement, CancellationToken ct = default);
        Task<ConsignmentSettlement?> GetSettlementByIdAsync(int settlementId, CancellationToken ct = default);

        Task AddPaymentAsync(ConsignmentPayment payment, CancellationToken ct = default);
    }
}

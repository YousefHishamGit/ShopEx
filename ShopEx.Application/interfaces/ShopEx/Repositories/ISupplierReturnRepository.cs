using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface ISupplierReturnRepository
    {
        Task<SupplierReturn?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<SupplierReturn?> GetByReturnNumberAsync(string returnNumber, CancellationToken ct = default);
        Task<List<SupplierReturn>> GetAllAsync(int? rmsSupplierId = null, SupplierReturnStatus? status = null, int? warehouseId = null, CancellationToken ct = default);
        Task<bool> ExistsAsync(string returnNumber, CancellationToken ct = default);
        Task<string> GetNextReturnNumberAsync(CancellationToken ct = default);
        Task AddAsync(SupplierReturn supplierReturn, CancellationToken ct = default);
        void Update(SupplierReturn supplierReturn);
        Task AddRefundAsync(SupplierRefund refund, CancellationToken ct = default);
        Task AddItemAsync(SupplierReturnItem item, CancellationToken ct = default);
        void RemoveItem(SupplierReturnItem item);
    }
}

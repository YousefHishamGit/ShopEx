using Microsoft.EntityFrameworkCore;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.ShopDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Repositories.ShopEx
{
    public class SupplierReturnRepository : ISupplierReturnRepository
    {
        private readonly ShopContext _db;

        public SupplierReturnRepository(ShopContext db)
        {
            _db = db;
        }

        public Task<SupplierReturn?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _db.SupplierReturns
                      .Include(r => r.Warehouse)
                      .Include(r => r.SupplierInvoice)
                      .Include(r => r.Items)
                      .Include(r => r.Refunds)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(r => r.ID == id, ct);
        }

        public Task<SupplierReturn?> GetByReturnNumberAsync(string returnNumber, CancellationToken ct = default)
        {
            return _db.SupplierReturns
                      .Include(r => r.Warehouse)
                      .Include(r => r.SupplierInvoice)
                      .Include(r => r.Items)
                      .Include(r => r.Refunds)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(r => r.ReturnNumber == returnNumber, ct);
        }

        public Task<List<SupplierReturn>> GetAllAsync(
            int? rmsSupplierId = null,
            SupplierReturnStatus? status = null,
            int? warehouseId = null,
            CancellationToken ct = default)
        {
            IQueryable<SupplierReturn> query = _db.SupplierReturns
                                                  .Include(r => r.Warehouse)
                                                  .Include(r => r.Items)
                                                  .Include(r => r.Refunds)
                                                  .AsSplitQuery();

            if (rmsSupplierId.HasValue)
            {
                query = query.Where(r => r.RMSSupplierID == rmsSupplierId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (warehouseId.HasValue)
            {
                query = query.Where(r => r.WarehouseID == warehouseId.Value);
            }

            return query.OrderByDescending(r => r.ReturnDate)
                        .ThenByDescending(r => r.ID)
                        .ToListAsync(ct);
        }

        public Task<bool> ExistsAsync(string returnNumber, CancellationToken ct = default)
        {
            return _db.SupplierReturns.AnyAsync(r => r.ReturnNumber == returnNumber, ct);
        }

        public async Task<string> GetNextReturnNumberAsync(CancellationToken ct = default)
        {
            var today = DateTime.Today;
            var prefix = $"RET-{today:yyyyMMdd}-";

            var lastReturn = await _db.SupplierReturns
                                      .Where(r => r.ReturnNumber.StartsWith(prefix))
                                      .OrderByDescending(r => r.ReturnNumber)
                                      .Select(r => r.ReturnNumber)
                                      .FirstOrDefaultAsync(ct);

            int nextSequence = 1;
            if (!string.IsNullOrEmpty(lastReturn))
            {
                var parts = lastReturn.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int seq))
                {
                    nextSequence = seq + 1;
                }
            }

            return $"{prefix}{nextSequence:D4}";
        }

        public async Task<Dictionary<int, decimal>> GetReturnedQuantitiesForInvoiceAsync(int supplierInvoiceId, CancellationToken ct = default)
        {
            var rows = await _db.SupplierReturnItems.AsNoTracking()
                .Where(i => i.SupplierReturn.SupplierInvoiceID == supplierInvoiceId
                            && i.SupplierReturn.Status != SupplierReturnStatus.Cancelled)
                .GroupBy(i => i.RMSItemID)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync(ct);
            return rows.ToDictionary(r => r.ItemId, r => r.Qty);
        }

        public async Task AddAsync(SupplierReturn supplierReturn, CancellationToken ct = default)
        {
            await _db.SupplierReturns.AddAsync(supplierReturn, ct);
        }

        public void Update(SupplierReturn supplierReturn)
        {
            _db.SupplierReturns.Update(supplierReturn);
        }

        public async Task AddRefundAsync(SupplierRefund refund, CancellationToken ct = default)
        {
            await _db.SupplierRefunds.AddAsync(refund, ct);
        }

        public async Task AddItemAsync(SupplierReturnItem item, CancellationToken ct = default)
        {
            await _db.SupplierReturnItems.AddAsync(item, ct);
        }

        public void RemoveItem(SupplierReturnItem item)
        {
            _db.SupplierReturnItems.Remove(item);
        }
    }
}

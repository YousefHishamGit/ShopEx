using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Device;
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
    public class DeviceRepository : IDeviceRepository
    {
        private readonly ShopContext _db;

        public DeviceRepository(ShopContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private IQueryable<Device> Filtered(DeviceFilter f)
        {
            var query = _db.Devices.AsNoTracking().AsQueryable();

            if (f.StoreId.HasValue)
            {
                // 0 = الإدارة العامة (HQ)
                query = f.StoreId.Value == 0
                    ? query.Where(d => d.StoreId == null)
                    : query.Where(d => d.StoreId == f.StoreId.Value);
            }

            if (f.Status.HasValue)
                query = query.Where(d => d.Status == f.Status.Value);

            if (f.FromDate.HasValue)
            {
                var from = f.FromDate.Value.Date;
                query = query.Where(d => d.PurchaseDate >= from);
            }

            if (f.ToDate.HasValue)
            {
                var toExclusive = f.ToDate.Value.Date.AddDays(1);
                query = query.Where(d => d.PurchaseDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(f.Search))
            {
                var s = f.Search.Trim();
                query = query.Where(d =>
                    d.Brand.Contains(s) ||
                    d.Model.Contains(s) ||
                    (d.Imei != null && d.Imei.Contains(s)) ||
                    (d.SellerName != null && d.SellerName.Contains(s)) ||
                    (d.BuyerName != null && d.BuyerName.Contains(s)) ||
                    (d.BuyerPhone != null && d.BuyerPhone.Contains(s)));
            }

            return query;
        }

        public Task<Device?> GetByIdAsync(int id, CancellationToken ct = default)
            => _db.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

        public Task<List<Device>> GetPageAsync(DeviceFilter filter, int skip, int take, CancellationToken ct = default)
        {
            return Filtered(filter)
                .OrderByDescending(d => d.PurchaseDate).ThenByDescending(d => d.Id)
                .Skip(skip).Take(take)
                .ToListAsync(ct);
        }

        public async Task<DeviceStatsDto> GetStatsAsync(DeviceFilter filter, CancellationToken ct = default)
        {
            var row = await Filtered(filter)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    InStock = g.Count(x => x.Status == DeviceStatus.InStock),
                    Sold = g.Count(x => x.Status == DeviceStatus.Sold),
                    Purchase = g.Sum(x => x.PurchasePrice),
                    InStockValue = g.Sum(x => x.Status == DeviceStatus.InStock ? x.PurchasePrice : 0m),
                    Sales = g.Sum(x => x.Status == DeviceStatus.Sold ? (x.SalePrice ?? 0m) : 0m),
                    SoldCost = g.Sum(x => x.Status == DeviceStatus.Sold ? x.PurchasePrice : 0m)
                })
                .FirstOrDefaultAsync(ct);

            if (row == null) return new DeviceStatsDto();

            return new DeviceStatsDto
            {
                TotalCount = row.Total,
                InStockCount = row.InStock,
                SoldCount = row.Sold,
                TotalPurchase = row.Purchase,
                InStockValue = row.InStockValue,
                TotalSales = row.Sales,
                TotalProfit = row.Sales - row.SoldCost
            };
        }

        public Task<bool> InStockImeiExistsAsync(string imei, int? excludeId, CancellationToken ct = default)
        {
            return _db.Devices.AsNoTracking().AnyAsync(d =>
                d.Imei == imei && d.Status == DeviceStatus.InStock && (excludeId == null || d.Id != excludeId), ct);
        }

        public async Task AddAsync(Device device, CancellationToken ct = default)
            => await _db.Devices.AddAsync(device, ct);

        public void Delete(Device device) => _db.Devices.Remove(device);
    }
}

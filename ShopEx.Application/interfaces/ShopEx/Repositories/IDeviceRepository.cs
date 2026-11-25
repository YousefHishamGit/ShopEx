using ShopEx.Application.DTOs.Device;
using ShopEx.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Repositories
{
    public interface IDeviceRepository
    {
        Task<Device?> GetByIdAsync(int id, CancellationToken ct = default);

        Task<List<Device>> GetPageAsync(DeviceFilter filter, int skip, int take, CancellationToken ct = default);

        Task<DeviceStatsDto> GetStatsAsync(DeviceFilter filter, CancellationToken ct = default);

        /// <summary>هل يوجد جهاز متاح (غير مباع) بنفس الـ IMEI؟</summary>
        Task<bool> InStockImeiExistsAsync(string imei, int? excludeId, CancellationToken ct = default);

        Task AddAsync(Device device, CancellationToken ct = default);

        void Delete(Device device);
    }
}

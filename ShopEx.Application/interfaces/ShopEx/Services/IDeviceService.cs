using ShopEx.Application.DTOs.Device;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.interfaces.ShopEx.Services
{
    public interface IDeviceService
    {
        Task<DeviceSummaryDto> GetSummaryAsync(DeviceFilter filter, int page, int pageSize, CancellationToken ct = default);
        Task<DeviceResponse?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken ct = default);
        Task<DeviceResponse> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken ct = default);
        Task<DeviceResponse> SellAsync(int id, SellDeviceRequest request, CancellationToken ct = default);
        /// <summary>إلغاء بيع جهاز (تصحيح خطأ) - يرجع الجهاز لحالة "متاح"</summary>
        Task<DeviceResponse> CancelSaleAsync(int id, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<List<StoreOption>> GetStoresAsync(CancellationToken ct = default);
    }

    public class StoreOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

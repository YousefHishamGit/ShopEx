using ShopEx.Application.DTOs.Device;
using ShopEx.Application.interfaces.RMS.Repositories;
using ShopEx.Application.interfaces.ShopEx.Repositories;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Application.Services.ShopEx
{
    public class DeviceService : IDeviceService
    {
        private const int MaxPageSize = 100;
        private const decimal MaxAmount = 999_999_999m;

        private readonly IDeviceRepository _deviceRepo;
        private readonly IStoreRepository _storeRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExpenseService _expenseService;

        public DeviceService(
            IDeviceRepository deviceRepo, IStoreRepository storeRepo, IUnitOfWork unitOfWork, IExpenseService expenseService)
        {
            _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
            _storeRepo = storeRepo ?? throw new ArgumentNullException(nameof(storeRepo));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<DeviceSummaryDto> GetSummaryAsync(DeviceFilter filter, int page, int pageSize, CancellationToken ct = default)
        {
            filter ??= new DeviceFilter();
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            // الاستعلامات تتابعية لأنها تشترك في نفس الـ DbContext
            var stats = await _deviceRepo.GetStatsAsync(filter, ct);
            var totalPages = Math.Max(1, (int)Math.Ceiling(stats.TotalCount / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            var rows = await _deviceRepo.GetPageAsync(filter, (page - 1) * pageSize, pageSize, ct);
            var names = await _storeRepo.GetNamesAsync(ct);

            return new DeviceSummaryDto
            {
                Stats = stats,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                Items = rows.Select(d => Map(d, names)).ToList()
            };
        }

        public async Task<DeviceResponse?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, ct);
            return device == null ? null : Map(device, await _storeRepo.GetNamesAsync(ct));
        }

        public async Task<DeviceResponse> CreateAsync(CreateDeviceRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            int? storeId = request.StoreId is > 0 ? request.StoreId : null;
            if (storeId.HasValue && !await _storeRepo.ExistsAsync(storeId.Value, ct))
                throw new ArgumentException("الفرع المحدد غير موجود.");

            var imei = Clean(request.Imei, 50);
            if (imei != null && await _deviceRepo.InStockImeiExistsAsync(imei, null, ct))
                throw new InvalidOperationException("يوجد جهاز متاح بنفس الـ IMEI بالفعل.");

            var device = new Device
            {
                StoreId = storeId,
                Brand = Required(request.Brand, 100, "ماركة الجهاز"),
                Model = Required(request.Model, 150, "موديل الجهاز"),
                Color = Clean(request.Color, 50),
                Storage = Clean(request.Storage, 50),
                Imei = imei,
                BatteryHealth = ValidateBattery(request.BatteryHealth),
                HasBox = request.HasBox,
                Notes = Clean(request.Notes, 1000),
                PurchasePrice = ValidateAmount(request.PurchasePrice, "سعر الشراء"),
                PurchaseDate = request.PurchaseDate ?? DateTime.Now,
                SellerName = Clean(request.SellerName, 200),
                Status = DeviceStatus.InStock,
                CreatedBy = Clean(request.CreatedBy, 100),
                CreatedAt = DateTime.Now
            };

            // الجهاز ومصروف الشراء في معاملة واحدة: الدفع من الفرع = مصروف على الفرع
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _deviceRepo.AddAsync(device, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                await SyncPurchaseExpenseAsync(device, ct);
            }, ct);

            return Map(device, await _storeRepo.GetNamesAsync(ct));
        }

        public async Task<DeviceResponse> UpdateAsync(int id, UpdateDeviceRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var device = await _deviceRepo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"الجهاز برقم {id} غير موجود.");

            var imei = Clean(request.Imei, 50);
            if (imei != null && device.Status == DeviceStatus.InStock && await _deviceRepo.InStockImeiExistsAsync(imei, id, ct))
                throw new InvalidOperationException("يوجد جهاز متاح بنفس الـ IMEI بالفعل.");

            device.Brand = Required(request.Brand, 100, "ماركة الجهاز");
            device.Model = Required(request.Model, 150, "موديل الجهاز");
            device.Color = Clean(request.Color, 50);
            device.Storage = Clean(request.Storage, 50);
            device.Imei = imei;
            device.BatteryHealth = ValidateBattery(request.BatteryHealth);
            device.HasBox = request.HasBox;
            device.Notes = Clean(request.Notes, 1000);
            device.PurchasePrice = ValidateAmount(request.PurchasePrice, "سعر الشراء");
            device.PurchaseDate = request.PurchaseDate ?? device.PurchaseDate;
            device.SellerName = Clean(request.SellerName, 200);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _unitOfWork.SaveChangesAsync(ct);
                await SyncPurchaseExpenseAsync(device, ct);
            }, ct);
            return Map(device, await _storeRepo.GetNamesAsync(ct));
        }

        public async Task<DeviceResponse> SellAsync(int id, SellDeviceRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var device = await _deviceRepo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"الجهاز برقم {id} غير موجود.");

            if (device.Status == DeviceStatus.Sold)
                throw new InvalidOperationException("هذا الجهاز تم بيعه بالفعل.");

            int? soldStoreId = request.SoldStoreId is > 0 ? request.SoldStoreId : null;
            if (soldStoreId.HasValue && !await _storeRepo.ExistsAsync(soldStoreId.Value, ct))
                throw new ArgumentException("الفرع المحدد غير موجود.");

            device.SalePrice = ValidateAmount(request.SalePrice, "سعر البيع");
            device.SoldStoreId = soldStoreId;
            device.Status = DeviceStatus.Sold;
            device.SoldAt = DateTime.Now;
            device.BuyerName = Clean(request.BuyerName, 200);
            device.BuyerPhone = Clean(request.BuyerPhone, 30);
            device.SaleNotes = Clean(request.Notes, 1000);
            device.SoldBy = Clean(request.SoldBy, 100);

            await _unitOfWork.SaveChangesAsync(ct);
            return Map(device, await _storeRepo.GetNamesAsync(ct));
        }

        public async Task<DeviceResponse> CancelSaleAsync(int id, CancellationToken ct = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"الجهاز برقم {id} غير موجود.");

            if (device.Status != DeviceStatus.Sold)
                throw new InvalidOperationException("هذا الجهاز لم يُبَع أصلاً.");

            if (!string.IsNullOrEmpty(device.Imei) && await _deviceRepo.InStockImeiExistsAsync(device.Imei, id, ct))
                throw new InvalidOperationException("لا يمكن إلغاء البيع: يوجد جهاز متاح آخر بنفس الـ IMEI.");

            device.Status = DeviceStatus.InStock;
            device.SalePrice = null;
            device.SoldAt = null;
            device.BuyerName = null;
            device.BuyerPhone = null;
            device.SaleNotes = null;
            device.SoldBy = null;
            device.SoldStoreId = null;

            await _unitOfWork.SaveChangesAsync(ct);
            return Map(device, await _storeRepo.GetNamesAsync(ct));
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        {
            var device = await _deviceRepo.GetByIdAsync(id, ct);
            if (device == null) return false;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _expenseService.DeleteDeviceExpenseAsync(id, ct);
                _deviceRepo.Delete(device);
                await _unitOfWork.SaveChangesAsync(ct);
            }, ct);
            return true;
        }

        public async Task<List<StoreOption>> GetStoresAsync(CancellationToken ct = default)
        {
            var names = await _storeRepo.GetNamesAsync(ct);
            return names.OrderBy(n => n.Key).Select(n => new StoreOption { Id = n.Key, Name = n.Value }).ToList();
        }

        // ================================================================
        // Helpers
        // ================================================================

        private Task SyncPurchaseExpenseAsync(Device device, CancellationToken ct)
            => _expenseService.SyncDeviceExpenseAsync(
                device.Id,
                device.StoreId,
                device.PurchasePrice,
                device.PurchaseDate,
                device.SellerName,
                $"شراء جهاز {device.Brand} {device.Model}".Trim() + (string.IsNullOrEmpty(device.Imei) ? "" : $" (IMEI {device.Imei})"),
                device.CreatedBy,
                ct);

        private static string Required(string? value, int maxLength, string label)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                throw new ArgumentException($"{label} مطلوب.");
            if (trimmed.Length > maxLength)
                throw new ArgumentException($"{label} يجب ألا يتجاوز {maxLength} حرفاً.");
            return trimmed;
        }

        private static decimal ValidateAmount(decimal amount, string label)
        {
            var rounded = Math.Round(amount, 2);
            if (rounded <= 0)
                throw new ArgumentException($"{label} يجب أن يكون أكبر من الصفر.");
            if (rounded > MaxAmount)
                throw new ArgumentException($"{label} أكبر من الحد المسموح.");
            return rounded;
        }

        private static int? ValidateBattery(int? value)
        {
            if (value is null) return null;
            if (value < 0 || value > 100)
                throw new ArgumentException("صحة البطارية يجب أن تكون بين 0 و 100.");
            return value;
        }

        private static string? Clean(string? value, int maxLength)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed)) return null;
            return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
        }

        private static string StoreNameOf(Dictionary<int, string> names, int? storeId)
            => storeId.HasValue
                ? (names.TryGetValue(storeId.Value, out var n) ? n : $"فرع #{storeId}")
                : "الإدارة العامة (HQ)";

        private static DeviceResponse Map(Device d, Dictionary<int, string> storeNames) => new()
        {
            Id = d.Id,
            StoreId = d.StoreId,
            StoreName = StoreNameOf(storeNames, d.StoreId),
            Brand = d.Brand,
            Model = d.Model,
            Color = d.Color,
            Storage = d.Storage,
            Imei = d.Imei,
            BatteryHealth = d.BatteryHealth,
            HasBox = d.HasBox,
            Notes = d.Notes,
            PurchasePrice = d.PurchasePrice,
            PurchaseDate = d.PurchaseDate,
            SellerName = d.SellerName,
            Status = d.Status,
            SalePrice = d.SalePrice,
            SoldAt = d.SoldAt,
            BuyerName = d.BuyerName,
            BuyerPhone = d.BuyerPhone,
            SaleNotes = d.SaleNotes,
            SoldBy = d.SoldBy,
            SoldStoreId = d.SoldStoreId,
            SoldStoreName = d.Status == DeviceStatus.Sold ? StoreNameOf(storeNames, d.SoldStoreId) : null,
            Profit = d.Status == DeviceStatus.Sold && d.SalePrice.HasValue ? d.SalePrice.Value - d.PurchasePrice : null,
            CreatedBy = d.CreatedBy,
            CreatedAt = d.CreatedAt
        };
    }
}

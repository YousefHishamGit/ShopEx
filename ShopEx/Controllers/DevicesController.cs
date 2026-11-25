using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Device;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using ShopEx.Security;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    /// <summary>
    /// أجهزة (موبايلات) يشتريها الـ HQ أو الفروع بغرض البيع.
    /// الأدمن يرى كل الأجهزة. موظف الفرع يرى الأجهزة المتاحة (غير المباعة) فقط من كل الفروع والـ HQ،
    /// ويقدر يبيع أي جهاز متاح (اللي يجيب أحسن سعر يبيعه)، لكن يسجّل ويعدّل أجهزة فرعه فقط.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceService _deviceService;

        public DevicesController(IDeviceService deviceService)
        {
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
        }

        /// <summary>الأدمن يتعامل مع أي جهاز، والموظف مع أجهزة فرعه فقط.</summary>
        private bool CanModify(int? deviceStoreId)
            => User.IsAdmin() || (deviceStoreId.HasValue && deviceStoreId == User.StoreId());

        /// <summary>قائمة الفروع (للفلتر ولاختيار الفرع عند الأدمن)</summary>
        [HttpGet("stores")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        public async Task<IActionResult> GetStores(CancellationToken ct)
            => Ok(await _deviceService.GetStoresAsync(ct));

        /// <summary>كل الأجهزة (صفحة واحدة) + إحصائيات لكل النتائج المطابقة للفلاتر</summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(DeviceSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] DeviceFilter filter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25,
            CancellationToken ct = default)
        {
            filter ??= new DeviceFilter();
            // الفروع ترى الأجهزة المتاحة فقط (المباع للأدمن فقط)
            if (!User.IsAdmin()) filter.Status = DeviceStatus.InStock;

            var result = await _deviceService.GetSummaryAsync(filter, page, pageSize, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var device = await _deviceService.GetByIdAsync(id, ct);
            if (device == null || (!User.IsAdmin() && device.Status == DeviceStatus.Sold))
                return NotFound(new { message = $"الجهاز برقم {id} غير موجود." });
            return Ok(device);
        }

        /// <summary>تسجيل شراء جهاز. الموظف: على فرعه تلقائياً. الأدمن: يختار الفرع أو HQ.</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateDeviceRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest(new { message = "بيانات الجهاز مطلوبة." });

            if (!User.IsAdmin())
            {
                if (User.StoreId() is not int ownStore) return Forbid();
                request.StoreId = ownStore;
            }
            request.CreatedBy = User.UserName();

            var result = await _deviceService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>تعديل بيانات الجهاز (الموظف: أجهزة فرعه غير المباعة فقط)</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest(new { message = "بيانات الجهاز مطلوبة." });

            var existing = await _deviceService.GetByIdAsync(id, ct);
            if (existing == null) return NotFound(new { message = $"الجهاز برقم {id} غير موجود." });

            if (!CanModify(existing.StoreId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكنك تعديل جهاز تابع لفرع آخر." });
            if (!User.IsAdmin() && existing.Status == DeviceStatus.Sold)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "لا يمكن تعديل جهاز تم بيعه." });

            return Ok(await _deviceService.UpdateAsync(id, request, ct));
        }

        /// <summary>
        /// تسجيل بيع الجهاز بسعر البيع. أي موظف يقدر يبيع أي جهاز متاح (حتى لو اشتراه فرع آخر)،
        /// والفرع البايع يُسجَّل من الـ Token. الأدمن يختار الفرع البايع أو HQ.
        /// </summary>
        [HttpPost("{id:int}/sell")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Sell(int id, [FromBody] SellDeviceRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest(new { message = "بيانات البيع مطلوبة." });

            var existing = await _deviceService.GetByIdAsync(id, ct);
            if (existing == null) return NotFound(new { message = $"الجهاز برقم {id} غير موجود." });

            if (!User.IsAdmin())
            {
                if (existing.Status == DeviceStatus.Sold)
                    return NotFound(new { message = $"الجهاز برقم {id} غير موجود." });
                if (User.StoreId() is not int ownStore) return Forbid();
                request.SoldStoreId = ownStore;
            }

            request.SoldBy = User.UserName();
            return Ok(await _deviceService.SellAsync(id, request, ct));
        }

        /// <summary>إلغاء بيع جهاز (تصحيح خطأ) - للأدمن فقط</summary>
        [HttpPost("{id:int}/cancel-sale")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(typeof(DeviceResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelSale(int id, CancellationToken ct)
            => Ok(await _deviceService.CancelSaleAsync(id, ct));

        /// <summary>حذف جهاز - للأدمن فقط</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = AppRoles.Admin)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            if (!await _deviceService.DeleteAsync(id, ct))
                return NotFound(new { message = $"الجهاز برقم {id} غير موجود." });
            return NoContent();
        }
    }
}

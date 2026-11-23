using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopEx.Application.DTOs.Balance;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Domain.Enum;
using ShopEx.Infrastructure.Persistence.RMS;
using ShopEx.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // الافتراضي: Admin فقط (FallbackPolicy). الموظف مسموح له فقط: رصيد فرعه (grouped/ledger) وتقفيل رصيد فرعه (close-store).
    public class BalancesController : ControllerBase
    {
        private readonly IBalanceService _balanceService;

        public BalancesController(IBalanceService balanceService)
        {
            _balanceService = balanceService ?? throw new ArgumentNullException(nameof(balanceService));
        }

        // ================================================================
        // 0. قائمة الفروع (Stores from RMS)
        // ================================================================
        /// <summary>
        /// استرجاع قائمة الفروع المسجلة في RMS للاختيار منها في الشحن والتقفيل
        /// </summary>
        [HttpGet("stores")]
        public async Task<IActionResult> GetStores([FromServices] RMSContext rmsContext, CancellationToken ct)
        {
            var stores = await rmsContext.Stores
                .AsNoTracking()
                .OrderBy(s => s.ID)
                .Select(s => new { id = s.ID, name = s.Name ?? $"فرع {s.ID}" })
                .ToListAsync(ct);
            return Ok(stores);
        }

        // ================================================================
        // 0.1 الأرصدة التراكمية المجمعة لكل فرع ووسيلة دفع (Grouped Balances)
        // ================================================================
        /// <summary>
        /// استرجاع رصيد كل فرع مجمعاً لكل وسيلة دفع (سطر واحد يجمع الـ 1000 والـ 1500 ليصبح 2500)
        /// </summary>
        [HttpGet("grouped")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(List<StoreBalanceGroupDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetGrouped(
            [FromQuery] int? storeId,
            [FromQuery] BalanceType? balanceType,
            CancellationToken ct)
        {
            if (!User.IsAdmin())
            {
                if (User.StoreId() is not int ownStore) return Forbid();
                storeId = ownStore;
            }

            var list = await _balanceService.GetGroupedBalancesAsync(storeId, balanceType, ct);
            return Ok(list);
        }

        // ================================================================
        // 0.2 تقفيل رصيد على مستوى الفرع والوسيلة مباشرة (Close by Store)
        // ================================================================
        /// <summary>
        /// تسجيل التقفيل اليومي للفرع مباشرة بدون الحاجة لتحديد شحنة معينة
        /// </summary>
        [HttpPost("close-store")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(List<BalanceClosureResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloseStore([FromBody] CloseStoreBalanceRequest request, CancellationToken ct)
        {
            if (request == null)
            {
                return BadRequest(new { message = "بيانات التقفيل مطلوبة." });
            }

            if (!User.IsAdmin())
            {
                if (User.StoreId() is not int ownStore) return Forbid();

                // الموظف يقفل رصيد فرعه فقط (من التوكن)، بوقت السيرفر، ويُسجل اسمه في الملاحظات
                request.StoreId = ownStore;
                request.ClosedAt = null;
                var note = request.Notes?.Trim();
                request.Notes = string.IsNullOrEmpty(note) ? $"[{User.UserName()}]" : $"[{User.UserName()}] {note}";
            }

            try
            {
                var result = await _balanceService.CloseByStoreAsync(request, ct);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // 0.3 كشف حركة العمليات والتحويلات التفصيلي (Transactions Ledger)
        // ================================================================
        /// <summary>
        /// استرجاع كشف الحركات والتحويلات التفصيلية (كل شحنة وتقفيل على حدة مرتبة زمنياً)
        /// </summary>
        [HttpGet("ledger")]
        [Authorize(Roles = AppRoles.AdminOrEmployee)]
        [ProducesResponseType(typeof(List<BalanceTransactionItemDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLedger(
            [FromQuery] int? storeId,
            [FromQuery] BalanceType? balanceType,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            if (!User.IsAdmin())
            {
                if (User.StoreId() is not int ownStore) return Forbid();
                storeId = ownStore;
            }

            var list = await _balanceService.GetLedgerAsync(storeId, balanceType, fromDate, toDate, ct);
            return Ok(list);
        }

        // ================================================================
        // 1. شحن رصيد للفرع (Charge Balance)
        // ================================================================
        /// <summary>
        /// شحن رصيد لفرع معين بوسيلة دفع محددة من الـ HQ
        /// </summary>
        [HttpPost("charge")]
        [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Charge([FromBody] ChargeBalanceRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _balanceService.ChargeAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // 2. تقفيل الرصيد المستخدم من الفرع (Close / Record Daily Usage)
        // ================================================================
        /// <summary>
        /// تسجيل التقفيل اليومي للرصيد الذي استخدمه الفرع فعلياً
        /// </summary>
        [HttpPost("{id:int}/closures")]
        [ProducesResponseType(typeof(BalanceClosureResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Close(int id, [FromBody] CloseBalanceRequest request, CancellationToken ct)
        {
            try
            {
                var result = await _balanceService.CloseAsync(id, request, ct);
                return CreatedAtAction(nameof(GetClosures), new { id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // 3. استرجاع قائمة الشحنات مع فلاتر (Get All Charges)
        // ================================================================
        /// <summary>
        /// استرجاع كل شحنات الأرصدة مع إمكانية الفلترة بالفرع ونوع الوسيلة والتاريخ
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<BalanceSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? storeId,
            [FromQuery] BalanceType? balanceType,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct)
        {
            var list = await _balanceService.GetAllAsync(storeId, balanceType, fromDate, toDate, ct);
            return Ok(list);
        }

        // ================================================================
        // 4. استرجاع شحنة رصيد مفصلة بالـ ID (Get Charge By ID)
        // ================================================================
        /// <summary>
        /// استرجاع تفاصيل شحنة رصيد معينة وسجل تقفيلاتها
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var result = await _balanceService.GetByIdAsync(id, ct);
            if (result == null)
            {
                return NotFound(new { message = $"شحنة الرصيد برقم {id} غير موجودة." });
            }

            return Ok(result);
        }

        // ================================================================
        // 5. استرجاع تقفيلات شحنة معينة (Get Closures For Charge)
        // ================================================================
        /// <summary>
        /// استرجاع كل التقفيلات الخاصة بشحنة رصيد معينة
        /// </summary>
        [HttpGet("{id:int}/closures")]
        [ProducesResponseType(typeof(List<BalanceClosureResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClosures(int id, CancellationToken ct)
        {
            var closures = await _balanceService.GetClosuresAsync(id, ct);
            return Ok(closures);
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Partners;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // الافتراضي: Admin فقط (FallbackPolicy) - الصفحة تعرض أرباح الفروع وحصص الشركاء.
    public class PartnersController : ControllerBase
    {
        private readonly IPartnerService _partnerService;

        public PartnersController(IPartnerService partnerService)
        {
            _partnerService = partnerService ?? throw new ArgumentNullException(nameof(partnerService));
        }

        /// <summary>
        /// تقرير الشركاء: أرباح كل فرع وزكاته وحصة كل شريك، وجدول الإيرادات والأرباح للفترة.
        /// </summary>
        /// <param name="fromDate">أول يوم (yyyy-MM-dd). الافتراضي: أول الشهر الحالي.</param>
        /// <param name="toDate">آخر يوم شامل (yyyy-MM-dd). الافتراضي: اليوم.</param>
        [HttpGet("report")]
        [ProducesResponseType(typeof(PartnersReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken ct)
        {
            var result = await _partnerService.GetReportAsync(fromDate, toDate, ct);
            return Ok(result);
        }

        /// <summary>تحديد شركاء الفرع ونسبهم ونسبة الزكاة (تستبدل القائمة الحالية).</summary>
        [HttpPut("branches/{storeId:int}")]
        [ProducesResponseType(typeof(BranchPartnersConfigDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SaveBranchPartners(int storeId, [FromBody] SaveBranchPartnersRequest request, CancellationToken ct)
        {
            var result = await _partnerService.SaveBranchPartnersAsync(storeId, request, ct);
            return Ok(result);
        }

        /// <summary>تفعيل/إغلاق كارت الإدارة العامة (HQ). الفروع دائماً مفعَّلة.</summary>
        [HttpPut("hq/enabled")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> SetHqEnabled([FromBody] SetEnabledRequest request, CancellationToken ct)
        {
            await _partnerService.SetHqEnabledAsync(request.IsEnabled, ct);
            return NoContent();
        }
    }
}

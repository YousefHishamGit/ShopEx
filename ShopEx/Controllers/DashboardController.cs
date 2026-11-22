using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShopEx.Application.DTOs.Dashboard;
using ShopEx.Application.interfaces.ShopEx.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // الافتراضي: Admin فقط (FallbackPolicy) - اللوحة تعرض أرقام كل الفروع والأرباح.
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
        }

        /// <summary>
        /// لوحة المتابعة: مبيعات وأرباح اليوم، الأجهزة المستلمة بالفروع، المصروفات،
        /// وفواتير الموردين/الأمانات المتأخرة والقريبة الاستحقاق.
        /// </summary>
        /// <param name="date">اليوم المطلوب (yyyy-MM-dd). الافتراضي: اليوم.</param>
        [HttpGet]
        [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] DateTime? date, CancellationToken ct)
        {
            var result = await _dashboardService.GetAsync(date, ct);
            return Ok(result);
        }
    }
}

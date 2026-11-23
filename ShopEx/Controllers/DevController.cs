using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.DTOs.Users;
using ShopEx.Application.Exceptions;
using ShopEx.Application.interfaces.ShopEx.Services;
using ShopEx.Security;

namespace ShopEx.Controllers
{
    /// <summary>
    /// لوحة المطوّر: تسجيل دخول المطوّر + التحكم اليدوي في الاشتراك.
    /// مقصور على Role = Dev (الـ Admin والـ Employee لا يصلون هنا).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = AppRoles.Dev)]
    public class DevController : ControllerBase
    {
        private readonly IDevAuthService _devAuth;
        private readonly ILicenseService _license;

        public DevController(IDevAuthService devAuth, ILicenseService license)
        {
            _devAuth = devAuth;
            _license = license;
        }

        // POST: api/Dev/login
        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [ProducesResponseType(typeof(DevLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            try
            {
                return Ok(await _devAuth.LoginAsync(request, ct));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (AccountLockedException ex)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = ex.Message });
            }
        }

        // GET: api/Dev/license
        [HttpGet("license")]
        [ProducesResponseType(typeof(LicenseResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLicense(CancellationToken ct)
            => Ok(await _license.GetAsync(ct));

        // PUT: api/Dev/license/enabled   { "isEnabled": true|false }
        [HttpPut("license/enabled")]
        [ProducesResponseType(typeof(LicenseResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetEnabled([FromBody] SetLicenseEnabledRequest request, CancellationToken ct)
            => Ok(await _license.SetEnabledAsync(request.IsEnabled, User.UserName(), ct));

        // POST: api/Dev/license/extend   { "months": 6 | 12 | ... }
        [HttpPost("license/extend")]
        [ProducesResponseType(typeof(LicenseResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Extend([FromBody] ExtendLicenseRequest request, CancellationToken ct)
            => Ok(await _license.ExtendAsync(request.Months, User.UserName(), ct));
    }
}

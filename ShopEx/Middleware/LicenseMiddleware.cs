using ShopEx.Application.DTOs.Licensing;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Security;

namespace ShopEx.Middleware
{
    /// <summary>
    /// يمنع أي مستخدم مسجّل (Admin/Employee) من استخدام الـ API لو الاشتراك منتهي أو متوقف.
    /// المطوّر (Role = Dev) مستثنى دائماً حتى يستطيع إعادة التفعيل. يُوضع بعد UseAuthentication.
    /// </summary>
    public sealed class LicenseMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ILicenseStateProvider licenses)
        {
            var user = context.User;
            var guarded = context.Request.Path.StartsWithSegments("/api")
                          && user.Identity?.IsAuthenticated == true
                          && !user.IsInRole(AppRoles.Dev);

            if (guarded)
            {
                var state = await licenses.GetAsync(context.RequestAborted);
                if (state == null || !state.IsValid(DateTime.UtcNow))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(
                        new { message = LicenseMessages.Blocked, code = LicenseMessages.BlockedCode },
                        context.RequestAborted);
                    return;
                }
            }

            await _next(context);
        }
    }
}

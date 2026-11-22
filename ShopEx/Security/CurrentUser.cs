using System.Globalization;
using System.Security.Claims;

namespace ShopEx.Security
{
    /// <summary>قراءة بيانات المستخدم الحالي من التوكن (لا نثق في أي قيمة قادمة من الـ Body/Query).</summary>
    public static class CurrentUser
    {
        public static int UserId(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirstValue(AppClaims.UserId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0;

        public static string UserName(this ClaimsPrincipal user)
            => user.FindFirstValue(AppClaims.Name) ?? string.Empty;

        public static int? StoreId(this ClaimsPrincipal user)
            => int.TryParse(user.FindFirstValue(AppClaims.StoreId), NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : null;

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(AppRoles.Admin);
    }
}

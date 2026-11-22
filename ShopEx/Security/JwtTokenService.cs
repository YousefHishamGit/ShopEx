using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ShopEx.Application.interfaces.ShopEx.Security;
using ShopEx.Domain.Entity;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShopEx.Security
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "ShopEx";
        public string Audience { get; set; } = "ShopEx.Client";

        /// <summary>مفتاح التوقيع (32 حرف على الأقل). لا يوضع في الـ Source Control: User Secrets أو Environment Variable.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>مدة صلاحية التوكن بالدقائق.</summary>
        public int ExpiryMinutes { get; set; } = 480;
    }

    /// <summary>أسماء الـ Claims المستخدمة في التوكن.</summary>
    public static class AppClaims
    {
        public const string UserId = "sub";
        public const string Name = "name";
        public const string Role = "role";
        public const string StoreId = "storeId";
        public const string SecurityStamp = "stamp";
    }

    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Employee = "Employee";
        public const string AdminOrEmployee = Admin + "," + Employee;
    }

    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;
        private readonly SigningCredentials _credentials;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            _credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
                SecurityAlgorithms.HmacSha256);
        }

        public (string Token, DateTime ExpiresAt) CreateToken(User user)
        {
            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.ExpiryMinutes);

            var claims = new List<Claim>
            {
                new(AppClaims.UserId, user.Id.ToString(CultureInfo.InvariantCulture)),
                new(AppClaims.Name, user.Name),
                new(AppClaims.Role, user.Role.ToString()),
                new(AppClaims.SecurityStamp, user.SecurityStamp.ToString("N")),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            // رقم الفرع داخل التوكن لمعرفة الموظف في أي فرع بالضبط
            if (user.StoreId.HasValue)
                claims.Add(new Claim(AppClaims.StoreId, user.StoreId.Value.ToString(CultureInfo.InvariantCulture)));

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: _credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expires.ToLocalTime());
        }
    }
}

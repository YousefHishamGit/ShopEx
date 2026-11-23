using ShopEx.Application.interfaces.ShopEx.Security;
using System;
using System.Security.Cryptography;

namespace ShopEx.Application.Services.ShopEx
{
    /// <summary>
    /// PBKDF2-SHA256 مع Salt عشوائي لكل كلمة مرور. الصيغة: PBKDF2-SHA256$iterations$saltBase64$hashBase64.
    /// عدد الـ iterations مخزن مع الـ Hash فيمكن رفعه مستقبلاً دون كسر الحسابات القديمة.
    /// </summary>
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const string Prefix = "PBKDF2-SHA256";
        private const int Iterations = 600_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public string Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public bool Verify(string hash, string password)
        {
            try
            {
                var parts = hash.Split('$');
                if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
                    return false;

                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

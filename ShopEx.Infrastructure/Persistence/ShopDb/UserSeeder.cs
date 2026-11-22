using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb
{
    public static class UserSeeder
    {
    
        public static async Task SeedInitialAdminAsync(
            ShopContext context,
            string? username,
            string? password,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (await context.Users.AnyAsync(ct))
                return;

           

            context.Users.Add(new User
            {
                Name = "مدير النظام",
                Phone = "0000000000",
                Role = UserRole.Admin,
                Username = "Admin User",
                Password = "Admin@123",
                StoreId = null,
                IsActive = true,
                SecurityStamp = Guid.NewGuid(),
                CreatedAt = DateTime.Now
            });
            await context.SaveChangesAsync(ct);

         
        }

        
    }
}

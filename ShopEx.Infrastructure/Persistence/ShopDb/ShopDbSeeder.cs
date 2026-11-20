using Microsoft.EntityFrameworkCore;
using ShopEx.Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb
{
    public static class ShopDbSeeder
    {
        public static async Task SeedAsync(ShopContext context, CancellationToken ct = default)
        {
            if (!await context.Warehouses.AnyAsync(ct))
            {
                var hqWarehouse = new Warehouse
                {
                    Name = "HQ Warehouse",
                    Location = "Main Office",
                    IsDefault = true,
                    IsActive = true
                };

                await context.Warehouses.AddAsync(hqWarehouse, ct);
                await context.SaveChangesAsync(ct);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");

            builder.HasKey(x => x.ID);

            builder.Property(x => x.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Location)
                   .HasMaxLength(300);

            builder.Property(x => x.IsDefault)
                   .IsRequired();

            builder.Property(x => x.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            // Relationships
            builder.HasMany(x => x.Balances)
                   .WithOne(b => b.Warehouse)
                   .HasForeignKey(b => b.WarehouseID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.StockMovements)
                   .WithOne(m => m.Warehouse)
                   .HasForeignKey(m => m.WarehouseID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.SupplierReturns)
                   .WithOne(r => r.Warehouse)
                   .HasForeignKey(r => r.WarehouseID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.Name)
                   .IsUnique();

            // Seed default warehouse
            builder.HasData(new Warehouse
            {
                ID = 1,
                Name = "HQ Warehouse",
                Location = "Main Office",
                IsDefault = true,
                IsActive = true
            });
        }
    }
}

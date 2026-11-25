using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable("Devices");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Brand).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Model).IsRequired().HasMaxLength(150);
            builder.Property(x => x.Color).HasMaxLength(50);
            builder.Property(x => x.Storage).HasMaxLength(50);
            builder.Property(x => x.Imei).HasMaxLength(50);
            builder.Property(x => x.Notes).HasMaxLength(1000);

            builder.Property(x => x.PurchasePrice).HasPrecision(18, 2).IsRequired();
            builder.Property(x => x.PurchaseDate).IsRequired().HasDefaultValueSql("GETDATE()");
            builder.Property(x => x.SellerName).HasMaxLength(200);

            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.SalePrice).HasPrecision(18, 2);
            builder.Property(x => x.BuyerName).HasMaxLength(200);
            builder.Property(x => x.BuyerPhone).HasMaxLength(30);
            builder.Property(x => x.SaleNotes).HasMaxLength(1000);
            builder.Property(x => x.SoldBy).HasMaxLength(100);

            builder.Property(x => x.CreatedBy).HasMaxLength(100);
            builder.Property(x => x.CreatedAt).IsRequired().HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasIndex(x => x.StoreId);
            builder.HasIndex(x => x.SoldStoreId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PurchaseDate);
            builder.HasIndex(x => x.SoldAt);   // dashboard sales by date
            builder.HasIndex(x => x.Imei);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
    {
        public void Configure(EntityTypeBuilder<StockBalance> builder)
        {
            builder.ToTable("StockBalances", t =>
            {
                t.HasCheckConstraint("CK_StockBalances_QuantityOnHand", "[QuantityOnHand] >= 0");
                t.HasCheckConstraint("CK_StockBalances_AverageCost", "[AverageCost] >= 0");
            });

            builder.HasKey(x => x.ID);

            builder.Property(x => x.WarehouseID)
                   .IsRequired();

            builder.Property(x => x.RMSItemID)
                   .IsRequired();

            builder.Property(x => x.QuantityOnHand)
                   .HasPrecision(18, 3)
                   .IsRequired();

            builder.Property(x => x.AverageCost)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.ReorderLevel)
                   .HasPrecision(18, 3);

            builder.Property(x => x.LastUpdatedAt)
                   .IsRequired();

            builder.Property(x => x.RowVersion)
                   .IsRowVersion();

            // Ignore computed properties
            builder.Ignore(x => x.TotalValue);
            builder.Ignore(x => x.IsLowStock);

            // Unique constraint: one balance record per item per warehouse
            builder.HasIndex(x => new { x.WarehouseID, x.RMSItemID })
                   .IsUnique();

            builder.HasIndex(x => x.RMSItemID);
        }
    }
}

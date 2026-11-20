using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
    {
        public void Configure(EntityTypeBuilder<StockMovement> builder)
        {
            builder.ToTable("StockMovements", t =>
            {
                t.HasCheckConstraint("CK_StockMovements_UnitCost", "[UnitCost] >= 0");
            });

            builder.HasKey(x => x.ID);

            builder.Property(x => x.WarehouseID)
                   .IsRequired();

            builder.Property(x => x.RMSItemID)
                   .IsRequired();

            builder.Property(x => x.MovementType)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 3)
                   .IsRequired();

            builder.Property(x => x.UnitCost)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.MovementDate)
                   .IsRequired();

            builder.Property(x => x.ReferenceType)
                   .HasMaxLength(50);

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            builder.Property(x => x.CreatedBy)
                   .HasMaxLength(150);

            // Ignore computed property
            builder.Ignore(x => x.TotalCost);

            // Indexes
            builder.HasIndex(x => new { x.RMSItemID, x.WarehouseID });
            builder.HasIndex(x => x.MovementDate);
            builder.HasIndex(x => new { x.ReferenceType, x.ReferenceID });
        }
    }
}

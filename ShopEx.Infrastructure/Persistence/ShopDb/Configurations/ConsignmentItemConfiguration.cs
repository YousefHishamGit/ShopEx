using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ConsignmentItemConfiguration : IEntityTypeConfiguration<ConsignmentItem>
    {
        public void Configure(EntityTypeBuilder<ConsignmentItem> builder)
        {
            builder.ToTable("ConsignmentItems");
            builder.HasKey(i => i.ID);

            builder.Property(i => i.RMSItemID).IsRequired();
            builder.Property(i => i.QuantityIssued).HasPrecision(18, 3);
            builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
            builder.Property(i => i.UnitCost).HasPrecision(18, 2);

            // Computed properties — not stored in DB
            builder.Ignore(i => i.QuantitySold);
            builder.Ignore(i => i.QuantityReturned);
            builder.Ignore(i => i.QuantityOutstanding);
            builder.Ignore(i => i.IssuedValue);
            builder.Ignore(i => i.SoldValue);
            builder.Ignore(i => i.OutstandingValue);
            builder.Ignore(i => i.Profit);

            builder.HasMany(i => i.SettlementItems)
                   .WithOne(si => si.ConsignmentItem)
                   .HasForeignKey(si => si.ConsignmentItemID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

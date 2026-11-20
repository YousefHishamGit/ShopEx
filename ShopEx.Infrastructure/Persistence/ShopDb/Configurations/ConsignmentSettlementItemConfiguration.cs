using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ConsignmentSettlementItemConfiguration : IEntityTypeConfiguration<ConsignmentSettlementItem>
    {
        public void Configure(EntityTypeBuilder<ConsignmentSettlementItem> builder)
        {
            builder.ToTable("ConsignmentSettlementItems");
            builder.HasKey(si => si.ID);

            builder.Property(si => si.QuantitySold).HasPrecision(18, 3);
            builder.Property(si => si.QuantityReturned).HasPrecision(18, 3);

            // SoldValue depends on navigation property (ConsignmentItem.UnitPrice)
            // Must be ignored — computed at application level
            builder.Ignore(si => si.SoldValue);
        }
    }
}

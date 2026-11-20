using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ConsignmentSettlementConfiguration : IEntityTypeConfiguration<ConsignmentSettlement>
    {
        public void Configure(EntityTypeBuilder<ConsignmentSettlement> builder)
        {
            builder.ToTable("ConsignmentSettlements");
            builder.HasKey(s => s.ID);

            builder.Property(s => s.SettlementDate).HasDefaultValueSql("GETDATE()");
            builder.Property(s => s.Notes).HasMaxLength(1000);
            builder.Property(s => s.CreatedBy).HasMaxLength(200);

            // Computed property — not stored in DB
            builder.Ignore(s => s.AmountDue);

            builder.HasMany(s => s.Items)
                   .WithOne(si => si.ConsignmentSettlement)
                   .HasForeignKey(si => si.ConsignmentSettlementID)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

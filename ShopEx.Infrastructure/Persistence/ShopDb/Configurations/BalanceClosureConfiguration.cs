using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class BalanceClosureConfiguration : IEntityTypeConfiguration<BalanceClosure>
    {
        public void Configure(EntityTypeBuilder<BalanceClosure> builder)
        {
            builder.ToTable("BalanceClosures");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BalanceId)
                   .IsRequired();

            builder.Property(x => x.AmountUsed)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.Profit)
                   .HasPrecision(18, 2)
                   .HasDefaultValue(0m)
                   .IsRequired();

            builder.Property(x => x.ClosedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            // Index: بحث سريع بالرصيد
            builder.HasIndex(x => x.BalanceId);

            // Index: بحث بالتاريخ
            builder.HasIndex(x => x.ClosedAt);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class BalanceConfiguration : IEntityTypeConfiguration<Balance>
    {
        public void Configure(EntityTypeBuilder<Balance> builder)
        {
            builder.ToTable("Balances");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.StoreId)
                   .IsRequired();

            builder.Property(x => x.BalanceType)
                   .IsRequired();

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.ChargedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            // علاقة 1-to-many مع التقفيلات
            builder.HasMany(x => x.Closures)
                   .WithOne(x => x.Balance)
                   .HasForeignKey(x => x.BalanceId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Index: بحث سريع بالفرع ونوع الوسيلة
            builder.HasIndex(x => new { x.StoreId, x.BalanceType });
            builder.HasIndex(x => x.ChargedAt);   // dashboard / ledger date ranges
        }
    }
}

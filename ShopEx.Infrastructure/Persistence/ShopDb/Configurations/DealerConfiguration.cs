using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class DealerConfiguration : IEntityTypeConfiguration<Dealer>
    {
        public void Configure(EntityTypeBuilder<Dealer> builder)
        {
            builder.ToTable("Dealers");
            builder.HasKey(d => d.ID);

            builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
            builder.Property(d => d.Phone).IsRequired().HasMaxLength(50);
            builder.Property(d => d.Address).HasMaxLength(500);
            builder.Property(d => d.Notes).HasMaxLength(1000);
            builder.Property(d => d.IsActive).HasDefaultValue(true);
            builder.Property(d => d.CreatedAt).HasDefaultValueSql("GETDATE()");

            builder.HasMany(d => d.Consignments)
                   .WithOne(c => c.Dealer)
                   .HasForeignKey(c => c.DealerID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

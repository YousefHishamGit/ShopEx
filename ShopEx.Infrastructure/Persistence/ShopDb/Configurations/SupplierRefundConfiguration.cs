using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class SupplierRefundConfiguration : IEntityTypeConfiguration<SupplierRefund>
    {
        public void Configure(EntityTypeBuilder<SupplierRefund> builder)
        {
            builder.ToTable("SupplierRefunds", t =>
            {
                t.HasCheckConstraint("CK_SupplierRefunds_Amount", "[Amount] > 0");
            });

            builder.HasKey(x => x.ID);

            builder.Property(x => x.SupplierReturnID)
                   .IsRequired();

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.RefundDate)
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .HasMaxLength(50);

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            // Relationship
            builder.HasOne(x => x.SupplierReturn)
                   .WithMany(r => r.Refunds)
                   .HasForeignKey(x => x.SupplierReturnID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.SupplierReturnID);
            builder.HasIndex(x => x.RefundDate);
        }
    }
}

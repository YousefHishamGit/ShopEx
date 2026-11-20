using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class SupplierReturnConfiguration : IEntityTypeConfiguration<SupplierReturn>
    {
        public void Configure(EntityTypeBuilder<SupplierReturn> builder)
        {
            builder.ToTable("SupplierReturns");

            builder.HasKey(x => x.ID);

            builder.Property(x => x.ReturnNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.RMSSupplierID)
                   .IsRequired();

            builder.Property(x => x.ReturnDate)
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.SettlementType)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            // Ignore computed properties
            builder.Ignore(x => x.TotalAmount);
            builder.Ignore(x => x.RefundedAmount);
            builder.Ignore(x => x.PendingRefund);

            // Relationships
            builder.HasMany(x => x.Items)
                   .WithOne(i => i.SupplierReturn)
                   .HasForeignKey(i => i.SupplierReturnID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Refunds)
                   .WithOne(r => r.SupplierReturn)
                   .HasForeignKey(r => r.SupplierReturnID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Optional link to supplier invoice
            builder.HasOne(x => x.SupplierInvoice)
                   .WithMany(si => si.Returns)
                   .HasForeignKey(x => x.SupplierInvoiceID)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.ReturnNumber)
                   .IsUnique();

            builder.HasIndex(x => new { x.RMSSupplierID, x.ReturnDate });
            builder.HasIndex(x => x.Status);
        }
    }
}

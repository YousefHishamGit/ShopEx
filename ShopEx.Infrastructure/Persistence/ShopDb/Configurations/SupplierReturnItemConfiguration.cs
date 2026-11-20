using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class SupplierReturnItemConfiguration : IEntityTypeConfiguration<SupplierReturnItem>
    {
        public void Configure(EntityTypeBuilder<SupplierReturnItem> builder)
        {
            builder.ToTable("SupplierReturnItems", t =>
            {
                t.HasCheckConstraint("CK_SupplierReturnItems_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_SupplierReturnItems_UnitPrice", "[UnitPrice] >= 0");
            });

            builder.HasKey(x => x.ID);

            builder.Property(x => x.SupplierReturnID)
                   .IsRequired();

            builder.Property(x => x.RMSItemID)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 3)
                   .IsRequired();

            builder.Property(x => x.UnitPrice)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.Reason)
                   .HasMaxLength(500);

            // Ignore computed property
            builder.Ignore(x => x.TotalPrice);

            // Relationship to SupplierReturn (Cascade defined from parent side, restated here for clarity)
            builder.HasOne(x => x.SupplierReturn)
                   .WithMany(r => r.Items)
                   .HasForeignKey(x => x.SupplierReturnID)
                   .OnDelete(DeleteBehavior.Cascade);

            // Optional link to original invoice item
            builder.HasOne(x => x.SupplierInvoiceItem)
                   .WithMany(sii => sii.ReturnItems)
                   .HasForeignKey(x => x.SupplierInvoiceItemID)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(x => x.SupplierReturnID);
            builder.HasIndex(x => x.RMSItemID);
            builder.HasIndex(x => x.SupplierInvoiceItemID);
        }
    }
}

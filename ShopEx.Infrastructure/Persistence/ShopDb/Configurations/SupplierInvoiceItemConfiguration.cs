using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class SupplierInvoiceItemConfiguration : IEntityTypeConfiguration<SupplierInvoiceItem>
    {
        public void Configure(EntityTypeBuilder<SupplierInvoiceItem> builder)
        {
            builder.ToTable("SupplierInvoiceItems", t =>
            {
                t.HasCheckConstraint("CK_SupplierInvoiceItems_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_SupplierInvoiceItems_UnitPrice", "[UnitPrice] >= 0");
            });

            builder.HasKey(x => x.ID);

            
            builder.Property(x => x.RMSItemID)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 3);

            builder.Property(x => x.UnitPrice)
                   .HasPrecision(18, 2);

            builder.Ignore(x => x.TotalPrice);

           

            builder.HasIndex(x => x.RMSItemID);   
        }
    }
}

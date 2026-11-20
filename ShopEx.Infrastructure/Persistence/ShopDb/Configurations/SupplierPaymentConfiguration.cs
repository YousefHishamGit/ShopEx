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
    public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
    {
        public void Configure(EntityTypeBuilder<SupplierPayment> builder)
        {
            builder.ToTable("SupplierPayments", t =>
            {
                t.HasCheckConstraint("CK_SupplierPayments_Amount", "[Amount] > 0");
            });

            builder.HasKey(x => x.ID);

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2);

            builder.Property(x => x.PaymentDate)
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .HasMaxLength(50);

            

            builder.HasIndex(x => x.PaymentDate);   
        }
    }
}

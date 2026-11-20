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
    public class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoice>
    {
        public void Configure(EntityTypeBuilder<SupplierInvoice> builder)
        {
            
            builder.ToTable("SupplierInvoices", t =>
            {
                t.HasCheckConstraint("CK_SupplierInvoices_TotalAmount", "[TotalAmount] >= 0");
                t.HasCheckConstraint("CK_SupplierInvoices_CreditDays", "[CreditDays] IS NULL OR [CreditDays] > 0");
            });

            builder.HasKey(x => x.ID);

          
            builder.Property(x => x.RMSSupplierID)
                   .IsRequired();

            builder.Property(x => x.InvoiceNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.InvoiceDate)
                   .IsRequired();

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            builder.Property(x => x.TotalAmount)
                   .HasPrecision(18, 2);

            builder.Property(x => x.SupplierInvoiceStatus)
                   .HasConversion<string>()
                   .IsRequired();

          
            builder.Property(x => x.CreditDays);
            builder.Property(x => x.DueDate);

            builder.Ignore(x => x.PaidAmount);
            builder.Ignore(x => x.RemainingAmount);

            
            builder.HasMany(x => x.Items)
                   .WithOne(i => i.SupplierInvoice)
                   .HasForeignKey(i => i.SupplierInvoiceID)
                   .OnDelete(DeleteBehavior.Cascade);

           
            builder.HasMany(x => x.Payments)
                   .WithOne(p => p.SupplierInvoice)
                   .HasForeignKey(p => p.SupplierInvoiceID)
                   .OnDelete(DeleteBehavior.Restrict);

           
            builder.HasIndex(x => new { x.RMSSupplierID, x.InvoiceNumber })
                   .IsUnique();

            
            builder.HasIndex(x => new { x.SupplierInvoiceStatus, x.DueDate });

            builder.HasIndex(x => x.InvoiceDate);
        }
    }
}

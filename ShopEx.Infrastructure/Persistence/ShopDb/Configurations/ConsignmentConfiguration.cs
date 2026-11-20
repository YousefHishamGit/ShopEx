using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;
using ShopEx.Domain.Enum;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ConsignmentConfiguration : IEntityTypeConfiguration<Consignment>
    {
        public void Configure(EntityTypeBuilder<Consignment> builder)
        {
            builder.ToTable("Consignments");
            builder.HasKey(c => c.ID);

            builder.Property(c => c.ConsignmentNumber)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(c => c.ConsignmentNumber).IsUnique();

            builder.Property(c => c.IssueDate).HasDefaultValueSql("GETDATE()");
            builder.Property(c => c.AgreedDays);
            builder.Property(c => c.DueDate).IsRequired();

            builder.Property(c => c.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .HasDefaultValue(ConsignmentStatus.Open);

            builder.Property(c => c.Notes).HasMaxLength(1000);

            // Computed properties — not stored in DB
            builder.Ignore(c => c.TotalIssuedValue);
            builder.Ignore(c => c.TotalSoldValue);
            builder.Ignore(c => c.PaidAmount);
            builder.Ignore(c => c.RemainingAmount);
            builder.Ignore(c => c.OutstandingQuantity);
            builder.Ignore(c => c.OutstandingValue);
            builder.Ignore(c => c.IsFullyAccountedFor);
            builder.Ignore(c => c.IsOverdue);

            builder.HasMany(c => c.Items)
                   .WithOne(i => i.Consignment)
                   .HasForeignKey(i => i.ConsignmentID)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Settlements)
                   .WithOne(s => s.Consignment)
                   .HasForeignKey(s => s.ConsignmentID)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(c => c.Payments)
                   .WithOne(p => p.Consignment)
                   .HasForeignKey(p => p.ConsignmentID)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

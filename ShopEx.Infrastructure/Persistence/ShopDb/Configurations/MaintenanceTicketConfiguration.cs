using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
    {
        public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
        {
            builder.ToTable("MaintenanceTickets", t =>
            {
                t.HasCheckConstraint("CK_MaintenanceTickets_ExpectedCost", "[ExpectedCost] >= 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TicketNumber)
                   .IsRequired()
                   .HasMaxLength(50);
            builder.Property(x => x.ReceivedBy).IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.CustomerName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.CustomerPhone)
                   .IsRequired()
                   .HasMaxLength(25);

            builder.Property(x => x.DeviceName)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.Brand)
                   .HasMaxLength(100);

            builder.Property(x => x.Model)
                   .HasMaxLength(100);

            builder.Property(x => x.Color)
                   .HasMaxLength(50);

            builder.Property(x => x.SerialNumber)
                   .HasMaxLength(100);

            builder.Property(x => x.IssueDescription)
                   .HasMaxLength(1000);

            builder.Property(x => x.Accessories)
                   .HasMaxLength(500);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.ReceivedAt)
                   .IsRequired();

            builder.Property(x => x.ExpectedCost)
                   .HasPrecision(18, 2);

            builder.Property(x => x.Status)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            // المرحلة الثانية: تفاصيل إتمام الإصلاح
            builder.Property(x => x.RepairNotes)
                   .HasMaxLength(1000);

            // المرحلة الثالثة: التسليم
            builder.Property(x => x.DeliveryNotes)
                   .HasMaxLength(1000);

            builder.Property(x => x.WarrantyNote)
                   .HasMaxLength(300);

            // الخصائص المحسوبة (Calculated Properties)
            builder.Ignore(x => x.TotalCost);
            builder.Ignore(x => x.PaidAmount);
            builder.Ignore(x => x.RemainingAmount);
            builder.Ignore(x => x.IsPaid);

            // العلاقات
            builder.HasMany(x => x.Operations)
                   .WithOne(o => o.MaintenanceTicket)
                   .HasForeignKey(o => o.MaintenanceTicketId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Payments)
                   .WithOne(p => p.MaintenanceTicket)
                   .HasForeignKey(p => p.MaintenanceTicketId)
                   .OnDelete(DeleteBehavior.Restrict);

            // الفهارس
            builder.HasIndex(x => x.TicketNumber)
                   .IsUnique();

            builder.HasIndex(x => x.CustomerPhone);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.StoreId);
            builder.HasIndex(x => x.ReceivedAt);
        }
    }
}

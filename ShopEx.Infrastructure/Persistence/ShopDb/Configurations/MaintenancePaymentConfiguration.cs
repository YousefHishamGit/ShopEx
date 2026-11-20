using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class MaintenancePaymentConfiguration : IEntityTypeConfiguration<MaintenancePayment>
    {
        public void Configure(EntityTypeBuilder<MaintenancePayment> builder)
        {
            builder.ToTable("MaintenancePayments", t =>
            {
                t.HasCheckConstraint("CK_MaintenancePayments_Amount", "[Amount] > 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.PaymentMethod)
                   .HasConversion<string>()
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(x => x.PaidAt)
                   .IsRequired();

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            builder.HasIndex(x => x.MaintenanceTicketId);
            builder.HasIndex(x => x.PaidAt);
        }
    }
}

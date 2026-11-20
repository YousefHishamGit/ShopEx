using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ConsignmentPaymentConfiguration : IEntityTypeConfiguration<ConsignmentPayment>
    {
        public void Configure(EntityTypeBuilder<ConsignmentPayment> builder)
        {
            builder.ToTable("ConsignmentPayments");
            builder.HasKey(p => p.ID);

            builder.Property(p => p.Amount).HasPrecision(18, 2);
            builder.Property(p => p.PaymentDate).HasDefaultValueSql("GETDATE()");
            builder.Property(p => p.PaymentMethod).HasMaxLength(100);
            builder.Property(p => p.Notes).HasMaxLength(1000);
        }
    }
}

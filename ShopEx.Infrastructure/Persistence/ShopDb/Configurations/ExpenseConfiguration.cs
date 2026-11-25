using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.ToTable("Expenses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ExpenseCategoryId)
                   .IsRequired();

            builder.Property(x => x.Amount)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.ExpenseDate)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.PaymentMethod)
                   .IsRequired();

            builder.Property(x => x.Recipient)
                   .HasMaxLength(200);

            builder.Property(x => x.ReceiptNumber)
                   .HasMaxLength(100);

            builder.Property(x => x.Notes)
                   .HasMaxLength(1000);

            builder.Property(x => x.CreatedBy)
                   .HasMaxLength(100);

            builder.Property(x => x.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            // Indexes للبحث السريع
            builder.HasIndex(x => x.ExpenseCategoryId);
            builder.HasIndex(x => x.StoreId);
            builder.HasIndex(x => x.ExpenseDate);

            // روابط المصروفات التلقائية (بدون FK: الارتباط منطقي فقط)
            builder.Ignore(x => x.IsAutomatic);
            builder.HasIndex(x => x.DeviceId);
            builder.HasIndex(x => x.BalanceId);
        }
    }
}

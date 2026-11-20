using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
    {
        public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
        {
            builder.ToTable("ExpenseCategories");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                   .IsRequired()
                   .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            // اسم البند فريد لمنع التكرار
            builder.HasIndex(x => x.Name)
                   .IsUnique();

            // علاقة 1-to-many مع المصروفات
            builder.HasMany(x => x.Expenses)
                   .WithOne(x => x.ExpenseCategory)
                   .HasForeignKey(x => x.ExpenseCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

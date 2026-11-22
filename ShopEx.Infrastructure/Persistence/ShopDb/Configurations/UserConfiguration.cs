using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", t =>
            {
                // الموظف لازم يكون مربوط بفرع
                t.HasCheckConstraint("CK_Users_EmployeeHasStore", "[Role] = 1 OR [StoreId] IS NOT NULL");
            });

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Name).IsRequired().HasMaxLength(150);
            builder.Property(u => u.Phone).IsRequired().HasMaxLength(30);
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Password).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Role).HasConversion<int>().IsRequired();
            builder.Property(u => u.IsActive).HasDefaultValue(true);
            builder.Property(u => u.SecurityStamp).IsRequired();
            builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

            // اسم المستخدم فريد (الـ Collation الافتراضي في SQL Server غير حساس لحالة الأحرف)
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.StoreId);
        }
    }
}

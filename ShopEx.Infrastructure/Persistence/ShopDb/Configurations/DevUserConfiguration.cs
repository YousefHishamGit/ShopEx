using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class DevUserConfiguration : IEntityTypeConfiguration<DevUser>
    {
        public void Configure(EntityTypeBuilder<DevUser> builder)
        {
            builder.ToTable("DevUsers");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
            builder.Property(d => d.Username).IsRequired().HasMaxLength(50);
            builder.Property(d => d.PasswordHash).IsRequired().HasMaxLength(200);
            builder.Property(d => d.IsActive).HasDefaultValue(true);
            builder.Property(d => d.SecurityStamp).IsRequired();
            builder.Property(d => d.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            builder.HasIndex(d => d.Username).IsUnique();
        }
    }
}

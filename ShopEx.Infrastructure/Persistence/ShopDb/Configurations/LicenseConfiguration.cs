using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class LicenseConfiguration : IEntityTypeConfiguration<License>
    {
        public void Configure(EntityTypeBuilder<License> builder)
        {
            // صف واحد فقط: Id ثابت = 1 (لا يُولَّد تلقائياً) + قيد يمنع أي صف آخر
            builder.ToTable("Licenses", t => t.HasCheckConstraint("CK_Licenses_Singleton", "[Id] = 1"));
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).ValueGeneratedNever();

            builder.Property(l => l.IsEnabled).IsRequired();
            builder.Property(l => l.ExpiresAt).IsRequired();
            builder.Property(l => l.UpdatedAt).IsRequired();
            builder.Property(l => l.UpdatedBy).HasMaxLength(50);
            builder.Property(l => l.RowVersion).IsRowVersion();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class BranchPartnerConfiguration : IEntityTypeConfiguration<BranchPartner>
    {
        public void Configure(EntityTypeBuilder<BranchPartner> builder)
        {
            builder.ToTable("BranchPartners");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(x => x.Percent)
                   .HasPrecision(5, 2)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired()
                   .HasDefaultValueSql("GETDATE()");

            builder.HasIndex(x => x.StoreId);
        }
    }

    public class BranchPartnerSettingConfiguration : IEntityTypeConfiguration<BranchPartnerSetting>
    {
        public void Configure(EntityTypeBuilder<BranchPartnerSetting> builder)
        {
            builder.ToTable("BranchPartnerSettings");
            builder.HasKey(x => x.StoreId);
            builder.Property(x => x.StoreId).ValueGeneratedNever();

            builder.Property(x => x.ZakatPercent)
                   .HasPrecision(5, 2)
                   .IsRequired();

            builder.Property(x => x.IsEnabled)
                   .IsRequired()
                   .HasDefaultValue(true);
        }
    }
}

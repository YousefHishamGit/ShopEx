using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopEx.Domain.Entity;

namespace ShopEx.Infrastructure.Persistence.ShopDb.Configurations
{
    public class MaintenanceTicketOperationConfiguration : IEntityTypeConfiguration<MaintenanceTicketOperation>
    {
        public void Configure(EntityTypeBuilder<MaintenanceTicketOperation> builder)
        {
            builder.ToTable("MaintenanceTicketOperations", t =>
            {
                t.HasCheckConstraint("CK_MaintenanceTicketOperations_Cost", "[Cost] >= 0");
            });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.OperationName)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(x => x.Cost)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.HasIndex(x => x.MaintenanceTicketId);
        }
    }
}

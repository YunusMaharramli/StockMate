using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class ShipmentStatusHistoryConfiguration : IEntityTypeConfiguration<ShipmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShipmentStatusHistory> builder)
    {
        builder.ToTable("ShipmentStatusHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.PreviousStatus).HasConversion<int?>();
        builder.Property(history => history.NewStatus).HasConversion<int>().IsRequired();
        builder.Property(history => history.ChangedAtUtc).IsRequired();
        builder.HasIndex(history => history.ShipmentId);

        builder.HasOne<OrderShipment>().WithMany()
            .HasForeignKey(history => history.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Staff>().WithMany()
            .HasForeignKey(history => history.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

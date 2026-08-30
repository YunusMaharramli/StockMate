using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class OrderShipmentConfiguration : IEntityTypeConfiguration<OrderShipment>
{
    public void Configure(EntityTypeBuilder<OrderShipment> builder)
    {
        builder.ToTable("OrderShipments");
        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Status).HasConversion<int>().IsRequired();
        builder.Property(shipment => shipment.CreatedAtUtc).IsRequired();
        builder.HasIndex(shipment => shipment.OrderId);
        builder.HasIndex(shipment => shipment.StaffId);
        builder.HasIndex(shipment => shipment.Status);

        builder.HasOne<Order>().WithMany()
            .HasForeignKey(shipment => shipment.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Staff>().WithMany()
            .HasForeignKey(shipment => shipment.StaffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

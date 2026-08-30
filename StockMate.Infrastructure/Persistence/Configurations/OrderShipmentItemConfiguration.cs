using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class OrderShipmentItemConfiguration : IEntityTypeConfiguration<OrderShipmentItem>
{
    public void Configure(EntityTypeBuilder<OrderShipmentItem> builder)
    {
        builder.ToTable("OrderShipmentItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Quantity).IsRequired();
        builder.HasIndex(item => new { item.ShipmentId, item.OrderItemId }).IsUnique();

        builder.HasOne<OrderShipment>().WithMany()
            .HasForeignKey(item => item.ShipmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<OrderItem>().WithMany()
            .HasForeignKey(item => item.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

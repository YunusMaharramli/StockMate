using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class SaleRecordConfiguration
    : IEntityTypeConfiguration<SaleRecord>
{
    public void Configure(
        EntityTypeBuilder<SaleRecord> builder)
    {
        builder.ToTable("SaleRecords");

        builder.HasKey(sale => sale.Id);

        builder.Property(sale => sale.OrderId)
            .IsRequired();

        builder.Property(sale => sale.OrderNumber)
            .IsRequired();

        builder.Property(sale => sale.DeliveryCompanyName)
            .HasMaxLength(200);

        builder.Property(sale => sale.SoldAtUtc)
            .IsRequired();

        builder.Property(sale => sale.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(sale => sale.TotalAmount);

        builder.HasIndex(sale => sale.OrderNumber);

        builder.HasIndex(sale => sale.SoldAtUtc);

        builder.HasIndex(sale => new
        {
            sale.DeliveryCompanyId,
            sale.SoldAtUtc
        });

        builder.HasIndex(sale => sale.ShipmentId)
            .IsUnique();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(sale => sale.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderShipment>()
            .WithMany()
            .HasForeignKey(sale => sale.ShipmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeliveryCompany>()
            .WithMany()
            .HasForeignKey(sale => sale.DeliveryCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sale => sale.Items)
            .WithOne()
            .HasForeignKey(item => item.SaleRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(sale => sale.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
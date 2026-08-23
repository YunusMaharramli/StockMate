using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration
    : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.ProductId)
            .IsRequired();

        builder.Property(movement => movement.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(movement => movement.PreviousQuantity)
            .IsRequired();

        builder.Property(movement => movement.NewQuantity)
            .IsRequired();

        builder.Property(movement => movement.QuantityChange)
            .IsRequired();

        builder.Property(movement => movement.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(movement => movement.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(movement => movement.ProductId);
        builder.HasIndex(movement => movement.OrderId);
        builder.HasIndex(movement => movement.CreatedAtUtc);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(movement => movement.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
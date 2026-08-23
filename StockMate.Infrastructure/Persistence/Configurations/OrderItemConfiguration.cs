using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.OrderId)
            .IsRequired();

        builder.Property(item => item.ProductId)
            .IsRequired();

        builder.Property(item => item.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ProductCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.FromStockQuantity)
            .IsRequired();

        builder.Property(item => item.SpecialOrderQuantity)
            .IsRequired();

        builder.Property(item => item.SpecialOrderStatus)
            .HasConversion<int>()
            .IsRequired();

        builder.Ignore(item => item.TotalQuantity);
        builder.Ignore(item => item.LineTotal);

        builder.HasIndex(item => new
        {
            item.OrderId,
            item.ProductId
        })
        .IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
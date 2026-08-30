using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class SaleRecordItemConfiguration
    : IEntityTypeConfiguration<SaleRecordItem>
{
    public void Configure(
        EntityTypeBuilder<SaleRecordItem> builder)
    {
        builder.ToTable("SaleRecordItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.SaleRecordId)
            .IsRequired();

        builder.Property(item => item.OrderItemId)
            .IsRequired();

        builder.Property(item => item.ProductId)
            .IsRequired();

        builder.Property(item => item.ProductName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ProductCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(item => item.LineTotal);

        builder.HasIndex(item => item.ProductId);

        builder.HasOne<OrderItem>()
            .WithMany()
            .HasForeignKey(item => item.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
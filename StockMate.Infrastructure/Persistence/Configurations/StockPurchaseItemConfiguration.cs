using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class StockPurchaseItemConfiguration
    : IEntityTypeConfiguration<StockPurchaseItem>
{
    public void Configure(
        EntityTypeBuilder<StockPurchaseItem> builder)
    {
        builder.ToTable("StockPurchaseItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.StockPurchaseId)
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

        builder.Property(item => item.UnitCost)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Ignore(item => item.LineTotal);

        builder.HasIndex(item => item.ProductId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
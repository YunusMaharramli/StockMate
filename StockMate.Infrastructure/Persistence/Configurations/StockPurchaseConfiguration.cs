using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class StockPurchaseConfiguration
    : IEntityTypeConfiguration<StockPurchase>
{
    public void Configure(
        EntityTypeBuilder<StockPurchase> builder)
    {
        builder.ToTable("StockPurchases");

        builder.HasKey(purchase => purchase.Id);

        builder.Property(purchase => purchase.PurchasedAtUtc)
            .IsRequired();

        builder.Property(purchase => purchase.Note)
            .HasMaxLength(1000);

        builder.Property(purchase => purchase.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(purchase => purchase.TotalAmount);

        builder.HasIndex(purchase => purchase.PurchasedAtUtc);

        builder.HasMany(purchase => purchase.Items)
            .WithOne()
            .HasForeignKey(item => item.StockPurchaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(purchase => purchase.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CustomerId)
            .IsRequired();

        builder.HasIndex(cart => cart.CustomerId)
            .IsUnique();

        builder.Property(cart => cart.CreatedAtUtc)
            .IsRequired();

        builder.Property(cart => cart.UpdatedAtUtc)
            .IsRequired();

        builder.Property(cart => cart.ExpiresAtUtc)
            .IsRequired();

        builder.Ignore(cart => cart.IsExpired);

        builder.HasOne<Customer>()
            .WithOne()
            .HasForeignKey<Cart>(cart => cart.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(cart => cart.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
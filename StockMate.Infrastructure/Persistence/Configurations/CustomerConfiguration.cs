using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.TelegramUserId)
            .IsRequired();

        builder.HasIndex(customer => customer.TelegramUserId)
            .IsUnique();

        builder.Property(customer => customer.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.LastName)
            .HasMaxLength(100);

        builder.Property(customer => customer.Username)
            .HasMaxLength(100);

        builder.Property(customer => customer.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(customer => customer.DeliveryAddress)
            .HasMaxLength(500);

        builder.Property(customer => customer.ProductPageSize)
            .HasDefaultValue(5)
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .IsRequired();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class CustomerAddressConfiguration
    : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(
        EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder.HasKey(address => address.Id);

        builder.Property(address => address.CustomerId)
            .IsRequired();

        builder.Property(address => address.AddressLine)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(address => address.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(address => address.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(address => new
        {
            address.CustomerId,
            address.IsActive
        });

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
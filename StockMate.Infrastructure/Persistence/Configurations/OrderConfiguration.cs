using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .UseIdentityByDefaultColumn()
            .ValueGeneratedOnAdd();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.CustomerId)
            .IsRequired();
        builder.Property(order => order.DeliveryCompanyId);

        builder.Property(order => order.DeliveryCompanyName)
            .HasMaxLength(200);

        builder.Property(order => order.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.DeliveryAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(order => order.DeliveryMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(order => order.DeliveryFee)
            .HasPrecision(18, 2);

        builder.Property(order => order.AdminNote)
            .HasMaxLength(1000);

        builder.Property(order => order.CancellationReason)
            .HasMaxLength(500);

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.Ignore(order => order.ProductsTotal);
        builder.Ignore(order => order.TotalAmount);
        builder.Ignore(order => order.ContainsSpecialOrder);
        builder.Ignore(order => order.FulfillmentStatus);
        builder.Ignore(order => order.RemainingQuantity);
        builder.Ignore(order => order.ReadyQuantity);

        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.CreatedAtUtc);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CustomerAddress>()
            .WithMany()
            .HasForeignKey(order => order.CustomerAddressId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DeliveryCompany>()
            .WithMany()
            .HasForeignKey(order => order.DeliveryCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

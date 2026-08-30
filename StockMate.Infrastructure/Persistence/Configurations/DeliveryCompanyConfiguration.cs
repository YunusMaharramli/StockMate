using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class DeliveryCompanyConfiguration
    : IEntityTypeConfiguration<DeliveryCompany>
{
    public void Configure(
        EntityTypeBuilder<DeliveryCompany> builder)
    {
        builder.ToTable("DeliveryCompanies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(company => company.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(company => company.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(company => company.Name)
            .IsUnique();

        builder.HasIndex(company => company.IsActive);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(category => category.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(category => category.NormalizedName)
            .IsUnique();

        builder.Property(category => category.IsActive)
            .IsRequired();

        builder.Property(category => category.IsArchived)
            .IsRequired();

        builder.Property(category => category.SortOrder)
            .IsRequired();

        builder.Property(category => category.CreatedAtUtc)
            .IsRequired();

        builder.Property(category => category.UpdatedAtUtc);

        builder.HasIndex(category => new
        {
            category.IsArchived,
            category.IsActive,
            category.SortOrder
        });
    }
}
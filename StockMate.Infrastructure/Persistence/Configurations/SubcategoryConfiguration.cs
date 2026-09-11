using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public sealed class SubcategoryConfiguration
    : IEntityTypeConfiguration<Subcategory>
{
    public void Configure(EntityTypeBuilder<Subcategory> builder)
    {
        builder.ToTable("Subcategories");

        builder.HasKey(subcategory => subcategory.Id);

        builder.HasAlternateKey(subcategory => new
        {
            subcategory.CategoryId,
            subcategory.Id
        });

        builder.Property(subcategory => subcategory.CategoryId)
            .IsRequired();

        builder.Property(subcategory => subcategory.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(subcategory => subcategory.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(subcategory => new
        {
            subcategory.CategoryId,
            subcategory.NormalizedName
        })
        .IsUnique();

        builder.Property(subcategory => subcategory.IsActive)
            .IsRequired();

        builder.Property(subcategory => subcategory.IsArchived)
            .IsRequired();

        builder.Property(subcategory => subcategory.SortOrder)
            .IsRequired();

        builder.Property(subcategory => subcategory.CreatedAtUtc)
            .IsRequired();

        builder.Property(subcategory => subcategory.UpdatedAtUtc);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(subcategory => subcategory.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(subcategory => new
        {
            subcategory.CategoryId,
            subcategory.IsArchived,
            subcategory.IsActive,
            subcategory.SortOrder
        });
    }
}

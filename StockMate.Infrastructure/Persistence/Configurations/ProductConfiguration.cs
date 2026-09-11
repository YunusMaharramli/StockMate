using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(product => product.Id);

        builder.Ignore(product => product.IsCatalogVisible);

        builder.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(product => product.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(product => product.Code)
            .IsUnique();

        builder.Property(product => product.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(product => product.StockQuantity)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(product => product.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(product => product.ImageReference)
            .HasMaxLength(1000);

        builder.Property(product => product.MinimumStockQuantity)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(product => product.IsActive)
            .IsRequired();

        builder.Property(product => product.IsArchived)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(product => product.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(product => product.Name);

        builder.HasIndex(product => product.IsActive);

        builder.HasIndex(product => new
        {
            product.CategoryId,
            product.SubcategoryId,
            product.IsArchived,
            product.IsActive
        });

        builder.HasOne(product => product.Category)
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(product => product.Subcategory)
            .WithMany()
            .HasForeignKey(product => new
            {
                product.CategoryId,
                product.SubcategoryId
            })
            .HasPrincipalKey(subcategory => new
            {
                subcategory.CategoryId,
                subcategory.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

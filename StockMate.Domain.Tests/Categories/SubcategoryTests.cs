using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.Categories;

public sealed class SubcategoryTests
{
    [Fact]
    public void Constructor_AssignsCategoryAndNormalizesName()
    {
        var categoryId = Guid.NewGuid();

        var subcategory = new Subcategory(categoryId, "  Nano  ", 3);

        Assert.Equal(categoryId, subcategory.CategoryId);
        Assert.Equal("Nano", subcategory.Name);
        Assert.Equal("NANO", subcategory.NormalizedName);
        Assert.Equal(3, subcategory.SortOrder);
    }

    [Fact]
    public void Constructor_RejectsEmptyCategoryId()
    {
        Assert.Throws<ArgumentException>(() =>
            new Subcategory(Guid.Empty, "Nano"));
    }
}

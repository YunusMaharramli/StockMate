using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void Constructor_NormalizesNameAndCreatesActiveCategory()
    {
        var category = new Category("  Mac  ", 2);

        Assert.Equal("Mac", category.Name);
        Assert.Equal("MAC", category.NormalizedName);
        Assert.Equal(2, category.SortOrder);
        Assert.True(category.IsActive);
        Assert.False(category.IsArchived);
    }

    [Fact]
    public void Archive_PreservesSafeHiddenState()
    {
        var category = new Category("Mac");

        category.Archive();

        Assert.True(category.IsArchived);
        Assert.False(category.IsActive);
        Assert.Throws<InvalidOperationException>(() => category.Activate());
    }

    [Fact]
    public void Restore_MakesCategoryActiveAgain()
    {
        var category = new Category("Mac");
        category.Archive();

        category.Restore();

        Assert.False(category.IsArchived);
        Assert.True(category.IsActive);
    }
}

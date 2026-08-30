using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.DeliveryCompanies;

public class DeliveryCompanyTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveCompany()
    {
        var company = new DeliveryCompany("Bolt Delivery");

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Bolt Delivery", company.Name);
        Assert.True(company.IsActive);
        Assert.NotEqual(default, company.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new DeliveryCompany(" "));
    }

    [Fact]
    public void Rename_ShouldChangeCompanyName()
    {
        var company = new DeliveryCompany("Köhnə ad");

        company.Rename("Yeni ad");

        Assert.Equal("Yeni ad", company.Name);
        Assert.NotNull(company.UpdatedAtUtc);
    }

    [Fact]
    public void Rename_ShouldThrow_WhenNameIsEmpty()
    {
        var company = new DeliveryCompany("Bolt Delivery");

        Assert.Throws<ArgumentException>(() =>
            company.Rename(" "));
    }

    [Fact]
    public void Deactivate_ShouldMakeCompanyInactive()
    {
        var company = new DeliveryCompany("Bolt Delivery");

        company.Deactivate();

        Assert.False(company.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeCompanyActiveAgain()
    {
        var company = new DeliveryCompany("Bolt Delivery");

        company.Deactivate();
        company.Activate();

        Assert.True(company.IsActive);
    }
}
using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.CustomerAddresses;

public class CustomerAddressTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveAddress()
    {
        var customerId = Guid.NewGuid();

        var address = new CustomerAddress(
            customerId,
            "Şəfayət Mehdiyev küçəsi 176",
            40.4093,
            49.8671);

        Assert.NotEqual(Guid.Empty, address.Id);
        Assert.Equal(customerId, address.CustomerId);
        Assert.Equal(
            "Şəfayət Mehdiyev küçəsi 176",
            address.AddressLine);
        Assert.Equal(40.4093, address.Latitude);
        Assert.Equal(49.8671, address.Longitude);
        Assert.True(address.IsActive);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAddressIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new CustomerAddress(
                Guid.NewGuid(),
                " "));
    }

    [Fact]
    public void Update_ShouldChangeAddressInformation()
    {
        var address = new CustomerAddress(
            Guid.NewGuid(),
            "Köhnə ünvan");

        address.Update(
            "Yeni ünvan",
            40.5,
            49.8);

        Assert.Equal("Yeni ünvan", address.AddressLine);
        Assert.Equal(40.5, address.Latitude);
        Assert.Equal(49.8, address.Longitude);
        Assert.NotNull(address.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_ShouldMakeAddressInactive()
    {
        var address = new CustomerAddress(
            Guid.NewGuid(),
            "Nizami küçəsi 42");

        address.Deactivate();

        Assert.False(address.IsActive);
    }

    [Fact]
    public void Activate_ShouldMakeAddressActiveAgain()
    {
        var address = new CustomerAddress(
            Guid.NewGuid(),
            "Nizami küçəsi 42");

        address.Deactivate();
        address.Activate();

        Assert.True(address.IsActive);
    }
}
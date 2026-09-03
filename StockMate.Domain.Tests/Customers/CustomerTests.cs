using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.Customers;

public sealed class CustomerTests
{
    [Fact]
    public void Block_ShouldDisableCustomerAccess()
    {
        var customer = CreateCustomer();

        customer.Block();

        Assert.True(customer.IsBlocked);
        Assert.NotNull(customer.BlockedAtUtc);
        Assert.NotNull(customer.UpdatedAtUtc);
    }

    [Fact]
    public void Reactivate_ShouldRestoreCustomerAccess()
    {
        var customer = CreateCustomer();
        customer.Block();

        customer.Reactivate();

        Assert.False(customer.IsBlocked);
        Assert.Null(customer.BlockedAtUtc);
        Assert.NotNull(customer.UpdatedAtUtc);
    }

    private static Customer CreateCustomer()
        => new(123456789, "Test", "Customer", "test_customer");
}

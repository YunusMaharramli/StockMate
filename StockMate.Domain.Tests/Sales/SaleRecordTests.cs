using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.Sales;

public class SaleRecordTests
{
    [Fact]
    public void AddItem_ShouldCalculateSalesTotal()
    {
        var order = CreateOrderWithProduct(
            quantity: 3,
            unitPrice: 1.80m);

        var orderItem = order.Items.Single();

        var sale = new SaleRecord(
            order,
            DateTime.UtcNow);

        sale.AddItem(
            orderItem,
            quantity: 2);

        Assert.Single(sale.Items);
        Assert.Equal(2, sale.Items.Single().Quantity);
        Assert.Equal(1.80m, sale.Items.Single().UnitPrice);
        Assert.Equal(3.60m, sale.TotalAmount);
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenItemIsRepeated()
    {
        var order = CreateOrderWithProduct(
            quantity: 3,
            unitPrice: 1.80m);

        var orderItem = order.Items.Single();

        var sale = new SaleRecord(
            order,
            DateTime.UtcNow);

        sale.AddItem(
            orderItem,
            quantity: 1);

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddItem(
                orderItem,
                quantity: 1));
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenItemBelongsToAnotherOrder()
    {
        var firstOrder = CreateOrderWithProduct(
            quantity: 2,
            unitPrice: 1.80m);

        var secondOrder = CreateOrderWithProduct(
            quantity: 2,
            unitPrice: 2.00m);

        var sale = new SaleRecord(
            firstOrder,
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddItem(
                secondOrder.Items.Single(),
                quantity: 1));
    }

    [Fact]
    public void Constructor_ShouldCopyDeliveryCompany()
    {
        var customerId = Guid.NewGuid();

        var order = new Order(
            customerId,
            "0501234567",
            "Müvəqqəti ünvan");

        var address = new CustomerAddress(
            customerId,
            "Nizami küçəsi 42");

        var company = new DeliveryCompany(
            "Bolt Delivery");

        order.SelectDeliveryDetails(
            address,
            company);

        var sale = new SaleRecord(
            order,
            DateTime.UtcNow);

        Assert.Equal(
            company.Id,
            sale.DeliveryCompanyId);

        Assert.Equal(
            "Bolt Delivery",
            sale.DeliveryCompanyName);
    }

    private static Order CreateOrderWithProduct(
        int quantity,
        decimal unitPrice)
    {
        var order = new Order(
            Guid.NewGuid(),
            "0501234567",
            "Nizami küçəsi 42");

        var product = new Product(
            "Coca-Cola 1 L",
            "CC-1L",
            unitPrice,
            stockQuantity: quantity,
            description: "Test məhsulu");

        order.AddItem(
            product,
            fromStockQuantity: quantity,
            specialOrderQuantity: 0);

        return order;
    }
}
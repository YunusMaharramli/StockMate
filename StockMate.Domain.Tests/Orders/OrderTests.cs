using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Domain.Tests.Orders;

public class OrderTests
{
    [Fact]
    public void Constructor_ShouldCreatePendingOrder()
    {
        var order = CreateOrder();

        Assert.Equal(
            OrderStatus.PendingAdminApproval,
            order.Status);

        Assert.Empty(order.Items);
    }

    [Fact]
    public void Confirm_ShouldThrow_WhenOrderIsEmpty()
    {
        var order = CreateOrder();

        Assert.Throws<InvalidOperationException>(
            order.Confirm);
    }

    [Fact]
    public void AddItem_ShouldCreatePriceSnapshot()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 10);

        order.AddItem(
            product,
            fromStockQuantity: 3,
            specialOrderQuantity: 0);

        product.ChangePrice(2.50m);

        var item = Assert.Single(order.Items);

        Assert.Equal(1.80m, item.UnitPrice);
        Assert.Equal(3, item.TotalQuantity);
        Assert.Equal(5.40m, item.LineTotal);
    }

    [Fact]
    public void AddItem_ShouldSeparateStockAndSpecialOrderQuantities()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 3);

        order.AddItem(
            product,
            fromStockQuantity: 3,
            specialOrderQuantity: 2);

        var item = Assert.Single(order.Items);

        Assert.Equal(3, item.FromStockQuantity);
        Assert.Equal(2, item.SpecialOrderQuantity);
        Assert.Equal(5, item.TotalQuantity);
        Assert.Equal(
            SpecialOrderStatus.Pending,
            item.SpecialOrderStatus);
        Assert.True(order.ContainsSpecialOrder);
    }

    [Fact]
    public void Statuses_ShouldChangeInRequiredOrder()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 10);

        order.AddItem(product, 2, 0);
        order.Confirm();
        order.MarkAsPreparing();
        order.DeliverReadyItems();

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.ConfirmedAtUtc);
        Assert.NotNull(order.CompletedAtUtc);
    }

    [Fact]
    public void MarkAsDelivering_ShouldThrow_WhenOrderIsNotPreparing()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 10);

        order.AddItem(product, 2, 0);

        Assert.Throws<InvalidOperationException>(
            order.MarkAsDelivering);
    }

    [Fact]
    public void Cancel_ShouldSetReasonAndStatus()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 10);

        order.AddItem(product, 2, 0);
        order.Confirm();
        order.Cancel("Customer cancelled the order.");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(
            "Customer cancelled the order.",
            order.CancellationReason);
        Assert.NotNull(order.CancelledAtUtc);
    }

    [Fact]
    public void CompletedOrder_ShouldNotBeCancelled()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 10);

        order.AddItem(product, 2, 0);
        order.Confirm();
        order.MarkAsPreparing();
        order.DeliverReadyItems();

        Assert.Throws<InvalidOperationException>(() =>
            order.Cancel("Invalid cancellation."));
    }

    [Fact]
    public void DeliverReadyItems_ShouldKeepRemainingSpecialQuantityOpen()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 3);

        order.AddItem(product, 3, 2);
        order.Confirm();
        order.MarkAsPreparing();
        order.SetDeliveryMode(DeliveryMode.PartialAllowed);

        order.DeliverReadyItems();

        var item = Assert.Single(order.Items);
        Assert.Equal(3, item.DeliveredQuantity);
        Assert.Equal(2, item.RemainingQuantity);
        Assert.Equal(FulfillmentStatus.PartiallyDelivered, order.FulfillmentStatus);
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public void DeliverReadyItems_ShouldCompleteAfterRemainingQuantityArrives()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 3);

        order.AddItem(product, 3, 2);
        order.Confirm();
        order.MarkAsPreparing();
        order.SetDeliveryMode(DeliveryMode.PartialAllowed);
        order.DeliverReadyItems();

        var item = Assert.Single(order.Items);
        item.MarkAsOrderedFromSupplier();
        item.MarkSpecialOrderAsArrived();
        order.DeliverReadyItems();

        Assert.Equal(5, item.DeliveredQuantity);
        Assert.Equal(0, item.RemainingQuantity);
        Assert.Equal(FulfillmentStatus.FullyDelivered, order.FulfillmentStatus);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public void CancelRemainingItems_ShouldClosePartiallyDeliveredOrder()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 3);

        order.AddItem(product, 3, 2);
        order.Confirm();
        order.MarkAsPreparing();
        order.SetDeliveryMode(DeliveryMode.PartialAllowed);
        order.DeliverReadyItems();

        order.CancelRemainingItems();

        Assert.Equal(FulfillmentStatus.ClosedWithShortage, order.FulfillmentStatus);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0, order.RemainingQuantity);
    }

    [Fact]
    public void DeliverReadyItems_ShouldWait_WhenAllTogetherIsSelected()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 3);

        order.AddItem(product, 3, 2);
        order.Confirm();
        order.MarkAsPreparing();
        order.SetDeliveryMode(DeliveryMode.AllTogether);

        Assert.Throws<InvalidOperationException>(order.DeliverReadyItems);
        Assert.Equal(0, Assert.Single(order.Items).DeliveredQuantity);
    }

    private static Order CreateOrder()
    {
        return new Order(
            customerId: Guid.NewGuid(),
            phoneNumber: "+994501234567",
            deliveryAddress: "Bakı şəhəri");
    }

    private static Product CreateProduct(int stockQuantity)
    {
        return new Product(
            name: "Coca-Cola 1 L",
            code: "CC-1L",
            price: 1.80m,
            stockQuantity,
            description: "Qazlı sərinləşdirici içki.");
    }
}

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

    [Fact]
    public void DeliverShipmentItems_ShouldDeliverExactQuantity()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 5);
        order.AddItem(product, 5, 0);
        order.Confirm();
        order.MarkAsPreparing();

        var item = Assert.Single(order.Items);
        order.DeliverShipmentItems(new Dictionary<Guid, int>
        {
            [item.Id] = 2
        });

        Assert.Equal(2, item.DeliveredQuantity);
        Assert.Equal(3, item.RemainingQuantity);
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public void DeliverShipmentItems_ShouldCompleteOrder_WhenEverythingIsDelivered()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 2);
        order.AddItem(product, 2, 0);
        order.Confirm();
        order.MarkAsPreparing();

        var item = Assert.Single(order.Items);
        order.DeliverShipmentItems(new Dictionary<Guid, int>
        {
            [item.Id] = 2
        });

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(FulfillmentStatus.FullyDelivered, order.FulfillmentStatus);
    }

    [Fact]
    public void DeliverShipmentItems_ShouldRejectQuantityGreaterThanReady()
    {
        var order = CreateOrder();
        var product = CreateProduct(stockQuantity: 2);
        order.AddItem(product, 2, 0);
        order.Confirm();
        order.MarkAsPreparing();

        var item = Assert.Single(order.Items);
        Assert.Throws<InvalidOperationException>(() =>
            order.DeliverShipmentItems(new Dictionary<Guid, int>
            {
                [item.Id] = 3
            }));
        Assert.Equal(0, item.DeliveredQuantity);
    }
    [Fact]
    public void SelectDeliveryDetails_ShouldSaveAddressAndCompany()
    {
        var customerId = Guid.NewGuid();

        var order = new Order(
            customerId,
            "0501234567",
            "Müvəqqəti ünvan");

        var address = new CustomerAddress(
            customerId,
            "Nizami küçəsi 42",
            40.4093,
            49.8671);

        var company = new DeliveryCompany(
            "Bolt Delivery");

        order.SelectDeliveryDetails(
            address,
            company);

        Assert.Equal(address.Id, order.CustomerAddressId);
        Assert.Equal(
            "Nizami küçəsi 42",
            order.DeliveryAddress);
        Assert.Equal(40.4093, order.Latitude);
        Assert.Equal(49.8671, order.Longitude);
        Assert.Equal(company.Id, order.DeliveryCompanyId);
        Assert.Equal(
            "Bolt Delivery",
            order.DeliveryCompanyName);
    }
    [Fact]
    public void SelectDeliveryDetails_ShouldThrow_WhenAddressBelongsToAnotherCustomer()
    {
        var order = new Order(
            Guid.NewGuid(),
            "0501234567",
            "Müvəqqəti ünvan");

        var address = new CustomerAddress(
            Guid.NewGuid(),
            "Başqa müştərinin ünvanı");

        var company = new DeliveryCompany(
            "Bolt Delivery");

        Assert.Throws<InvalidOperationException>(() =>
            order.SelectDeliveryDetails(
                address,
                company));
    }
    [Fact]
    public void SelectDeliveryDetails_ShouldThrow_WhenAddressIsInactive()
    {
        var customerId = Guid.NewGuid();

        var order = new Order(
            customerId,
            "0501234567",
            "Müvəqqəti ünvan");

        var address = new CustomerAddress(
            customerId,
            "Nizami küçəsi 42");

        address.Deactivate();

        var company = new DeliveryCompany(
            "Bolt Delivery");

        Assert.Throws<InvalidOperationException>(() =>
            order.SelectDeliveryDetails(
                address,
                company));
    }
    [Fact]
    public void SelectDeliveryDetails_ShouldThrow_WhenCompanyIsInactive()
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

        company.Deactivate();

        Assert.Throws<InvalidOperationException>(() =>
            order.SelectDeliveryDetails(
                address,
                company));
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

using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Domain.Tests.Shipments;

public class OrderShipmentTests
{
    [Fact]
    public void Constructor_ShouldCreateAssignedShipment()
    {
        var shipment = CreateShipment();
        Assert.Equal(ShipmentStatus.Assigned, shipment.Status);
        Assert.NotEqual(Guid.Empty, shipment.Id);
    }

    [Fact]
    public void Statuses_ShouldChangeInRequiredOrder()
    {
        var shipment = CreateShipment();
        shipment.StartPreparing();
        shipment.MarkReady();
        shipment.MarkOutForDelivery();
        shipment.MarkDelivered();
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
        Assert.NotNull(shipment.DeliveredAtUtc);
    }

    [Fact]
    public void MarkReady_ShouldThrow_WhenNotPreparing()
    {
        var shipment = CreateShipment();
        Assert.Throws<InvalidOperationException>(shipment.MarkReady);
    }

    [Fact]
    public void DeliveredShipment_ShouldNotBeCancelled()
    {
        var shipment = CreateShipment();
        shipment.StartPreparing();
        shipment.MarkReady();
        shipment.MarkOutForDelivery();
        shipment.MarkDelivered();
        Assert.Throws<InvalidOperationException>(shipment.Cancel);
    }

    private static OrderShipment CreateShipment()
        => new(Guid.NewGuid(), Guid.NewGuid());
}

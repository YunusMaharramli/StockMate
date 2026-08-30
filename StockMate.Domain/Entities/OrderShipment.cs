using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class OrderShipment
{
    private OrderShipment() { }

    public OrderShipment(Guid orderId, Guid staffId)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (staffId == Guid.Empty) throw new ArgumentException("Staff ID is required.", nameof(staffId));

        Id = Guid.NewGuid();
        OrderId = orderId;
        StaffId = staffId;
        Status = ShipmentStatus.Assigned;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid StaffId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PreparingAtUtc { get; private set; }
    public DateTime? ReadyAtUtc { get; private set; }
    public DateTime? OutForDeliveryAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public void StartPreparing()
    {
        EnsureStatus(ShipmentStatus.Assigned);
        Status = ShipmentStatus.Preparing;
        PreparingAtUtc = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        EnsureStatus(ShipmentStatus.Preparing);
        Status = ShipmentStatus.Ready;
        ReadyAtUtc = DateTime.UtcNow;
    }

    public void MarkOutForDelivery()
    {
        EnsureStatus(ShipmentStatus.Ready);
        Status = ShipmentStatus.OutForDelivery;
        OutForDeliveryAtUtc = DateTime.UtcNow;
    }

    public void MarkDelivered()
    {
        EnsureStatus(ShipmentStatus.OutForDelivery);
        Status = ShipmentStatus.Delivered;
        DeliveredAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is ShipmentStatus.Delivered or ShipmentStatus.Cancelled)
            throw new InvalidOperationException("Shipment cannot be cancelled.");

        Status = ShipmentStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    private void EnsureStatus(ShipmentStatus required)
    {
        if (Status != required)
            throw new InvalidOperationException($"Shipment must be in {required} status.");
    }
}

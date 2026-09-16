using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class ShipmentStatusHistory
{
    private ShipmentStatusHistory() { }

    public ShipmentStatusHistory(
        Guid shipmentId,
        Guid staffId,
        ShipmentStatus? previousStatus,
        ShipmentStatus newStatus)
    {
        if (shipmentId == Guid.Empty) throw new ArgumentException("ID доставки обязателен.", nameof(shipmentId));
        if (staffId == Guid.Empty) throw new ArgumentException("ID сотрудника обязателен.", nameof(staffId));

        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        StaffId = staffId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ShipmentId { get; private set; }
    public Guid StaffId { get; private set; }
    public ShipmentStatus? PreviousStatus { get; private set; }
    public ShipmentStatus NewStatus { get; private set; }
    public DateTime ChangedAtUtc { get; private set; }
}

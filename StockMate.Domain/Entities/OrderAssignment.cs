namespace StockMate.Domain.Entities;

public class OrderAssignment
{
    private OrderAssignment() { }

    public OrderAssignment(Guid orderId, Guid staffId, Guid assignedByStaffId)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("Order ID is required.", nameof(orderId));
        if (staffId == Guid.Empty) throw new ArgumentException("Staff ID is required.", nameof(staffId));
        if (assignedByStaffId == Guid.Empty) throw new ArgumentException("Assigning admin ID is required.", nameof(assignedByStaffId));

        Id = Guid.NewGuid();
        OrderId = orderId;
        StaffId = staffId;
        AssignedByStaffId = assignedByStaffId;
        AssignedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid StaffId { get; private set; }
    public Guid AssignedByStaffId { get; private set; }
    public DateTime AssignedAtUtc { get; private set; }
    public DateTime? ReleasedAtUtc { get; private set; }
    public bool IsActive => ReleasedAtUtc is null;

    public void Release()
    {
        if (!IsActive)
            throw new InvalidOperationException("Order assignment is already released.");
        ReleasedAtUtc = DateTime.UtcNow;
    }
}

namespace StockMate.Application.Staff.Models;

public sealed record OrderAssignmentDto(
    Guid Id,
    long OrderNumber,
    Guid StaffId,
    string StaffName,
    DateTime AssignedAtUtc);

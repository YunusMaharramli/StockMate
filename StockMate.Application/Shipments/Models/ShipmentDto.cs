using StockMate.Domain.Enums;

namespace StockMate.Application.Shipments.Models;

public sealed record ShipmentDto(
    Guid Id,
    long OrderNumber,
    Guid StaffId,
    string StaffName,
    ShipmentStatus Status,
    string PhoneNumber,
    string DeliveryAddress,
    double? Latitude,
    double? Longitude,
    IReadOnlyList<ShipmentItemDto> Items,
    DateTime CreatedAtUtc,
    DateTime? DeliveredAtUtc);

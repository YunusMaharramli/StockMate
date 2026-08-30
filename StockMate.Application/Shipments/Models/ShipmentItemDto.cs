namespace StockMate.Application.Shipments.Models;

public sealed record ShipmentItemDto(
    Guid OrderItemId,
    string ProductName,
    int Quantity);

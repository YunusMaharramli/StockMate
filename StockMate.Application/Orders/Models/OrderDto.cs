using StockMate.Domain.Enums;

namespace StockMate.Application.Orders.Models;

public sealed record OrderDto(
    Guid Id,
    long OrderNumber,
    Guid CustomerId,
    string PhoneNumber,
    string DeliveryAddress,
    double? Latitude,
    double? Longitude,
    OrderStatus Status,
    IReadOnlyList<OrderItemDto> Items,
    decimal ProductsTotal,
    decimal? DeliveryFee,
    decimal TotalAmount,
    bool ContainsSpecialOrder,
    string? AdminNote,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc);

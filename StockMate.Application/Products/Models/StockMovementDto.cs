using StockMate.Domain.Enums;

namespace StockMate.Application.Products.Models;

public sealed record StockMovementDto(
    Guid Id,
    StockMovementType Type,
    int PreviousQuantity,
    int NewQuantity,
    int QuantityChange,
    string Reason,
    Guid? OrderId,
    DateTime CreatedAtUtc);
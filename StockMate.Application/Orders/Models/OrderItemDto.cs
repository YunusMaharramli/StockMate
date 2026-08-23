using StockMate.Domain.Enums;

namespace StockMate.Application.Orders.Models;

public sealed record OrderItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal UnitPrice,
    int FromStockQuantity,
    int SpecialOrderQuantity,
    int TotalQuantity,
    decimal LineTotal,
    SpecialOrderStatus SpecialOrderStatus);

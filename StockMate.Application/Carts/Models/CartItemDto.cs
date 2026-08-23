using StockMate.Domain.Enums;

namespace StockMate.Application.Carts.Models;

public sealed record CartItemDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal UnitPrice,
    int RequestedQuantity,
    int FromStockQuantity,
    int SpecialOrderQuantity,
    int UnavailableQuantity,
    decimal LineTotal,
    bool IsActive,
    CartItemFulfillmentMode FulfillmentMode);
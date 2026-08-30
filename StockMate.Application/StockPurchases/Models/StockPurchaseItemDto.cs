namespace StockMate.Application.StockPurchases.Models;

public sealed record StockPurchaseItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int Quantity,
    decimal UnitCost,
    decimal LineTotal);
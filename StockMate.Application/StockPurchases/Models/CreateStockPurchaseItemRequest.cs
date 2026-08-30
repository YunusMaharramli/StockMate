namespace StockMate.Application.StockPurchases.Models;

public sealed record CreateStockPurchaseItemRequest(
    Guid ProductId,
    int Quantity,
    decimal UnitCost);
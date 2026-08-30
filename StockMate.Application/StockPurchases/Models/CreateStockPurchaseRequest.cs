namespace StockMate.Application.StockPurchases.Models;

public sealed record CreateStockPurchaseRequest(
    DateTime PurchasedAtUtc,
    string? Note,
    IReadOnlyList<CreateStockPurchaseItemRequest> Items);
namespace StockMate.Application.StockPurchases.Models;

public sealed record StockPurchaseDto(
    Guid Id,
    DateTime PurchasedAtUtc,
    string? Note,
    IReadOnlyList<StockPurchaseItemDto> Items,
    decimal TotalAmount,
    DateTime CreatedAtUtc);
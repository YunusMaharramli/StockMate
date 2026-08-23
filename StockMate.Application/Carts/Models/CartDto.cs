namespace StockMate.Application.Carts.Models;

public sealed record CartDto(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<CartItemDto> Items,
    decimal ProductsTotal,
    DateTime ExpiresAtUtc);
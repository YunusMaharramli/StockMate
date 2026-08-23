namespace StockMate.Application.Products.Models;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Code,
    decimal Price,
    int StockQuantity,
    string Description,
    string? ImageReference,
    bool IsActive);
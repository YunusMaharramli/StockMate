namespace StockMate.Application.Products.Models;

public sealed record ProductCatalogItemDto(
    Guid Id,
    string Name,
    string Code,
    decimal Price,
    string Description,
    string? ImageReference,
    Guid? CategoryId,
    string? CategoryName,
    Guid? SubcategoryId,
    string? SubcategoryName);

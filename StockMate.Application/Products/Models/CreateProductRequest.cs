namespace StockMate.Application.Products.Models;

public sealed record CreateProductRequest(
    string Name,
    string Code,
    decimal Price,
    int InitialStockQuantity,
    string Description,
    string? ImageReference,
    Guid? CategoryId = null,
    Guid? SubcategoryId = null,
    int MinimumStockQuantity = 0);

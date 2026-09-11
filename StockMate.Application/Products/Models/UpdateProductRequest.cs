namespace StockMate.Application.Products.Models;

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    string? ImageReference,
    Guid? CategoryId = null,
    Guid? SubcategoryId = null,
    int? MinimumStockQuantity = null,
    bool UpdateClassification = false);

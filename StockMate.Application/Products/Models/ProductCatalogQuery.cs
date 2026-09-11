namespace StockMate.Application.Products.Models;

public sealed record ProductCatalogQuery(
    int PageNumber = 1,
    int PageSize = 5,
    Guid? CategoryId = null,
    string? SubcategoryName = null,
    string? Search = null);

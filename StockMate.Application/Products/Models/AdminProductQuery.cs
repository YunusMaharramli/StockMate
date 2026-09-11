namespace StockMate.Application.Products.Models;

public sealed record AdminProductQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    Guid? SubcategoryId = null,
    bool? IsActive = null,
    bool? InStock = null,
    bool? LowStock = null,
    bool IncludeArchived = false);

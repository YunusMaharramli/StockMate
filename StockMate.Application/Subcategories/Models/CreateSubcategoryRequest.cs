namespace StockMate.Application.Subcategories.Models;

public sealed record CreateSubcategoryRequest(
    Guid CategoryId,
    string Name,
    int SortOrder = 0);
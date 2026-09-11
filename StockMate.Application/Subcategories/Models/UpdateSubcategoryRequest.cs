namespace StockMate.Application.Subcategories.Models;

public sealed record UpdateSubcategoryRequest(
    string Name,
    int SortOrder);
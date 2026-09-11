namespace StockMate.Application.Categories.Models;

public sealed record UpdateCategoryRequest(
    string Name,
    int SortOrder);
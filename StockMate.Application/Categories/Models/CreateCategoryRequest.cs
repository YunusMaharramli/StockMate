namespace StockMate.Application.Categories.Models;

public sealed record CreateCategoryRequest(
    string Name,
    int SortOrder = 0);
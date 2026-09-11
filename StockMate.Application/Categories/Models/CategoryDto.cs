namespace StockMate.Application.Categories.Models;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    bool IsActive,
    bool IsArchived,
    int SortOrder,
    int SubcategoryCount,
    int ProductCount);
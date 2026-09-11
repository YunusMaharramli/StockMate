namespace StockMate.Application.Subcategories.Models;

public sealed record SubcategoryDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    bool IsActive,
    bool IsArchived,
    int SortOrder,
    int ProductCount);
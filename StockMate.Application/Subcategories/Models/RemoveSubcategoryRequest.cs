namespace StockMate.Application.Subcategories.Models;

public enum SubcategoryRemovalMode
{
    MoveProducts,
    ArchiveSubcategory,
    ArchiveProducts
}

public sealed record RemoveSubcategoryRequest(
    SubcategoryRemovalMode Mode,
    Guid? TargetSubcategoryId = null);

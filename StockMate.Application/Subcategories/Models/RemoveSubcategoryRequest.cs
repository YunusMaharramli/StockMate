namespace StockMate.Application.Subcategories.Models;

public enum SubcategoryRemovalMode
{
    MoveProducts,
    ArchiveSubcategory,
    ArchiveProducts,
    DeleteProducts
}

public sealed record RemoveSubcategoryRequest(
    SubcategoryRemovalMode Mode,
    Guid? TargetSubcategoryId = null);

namespace StockMate.Application.Categories.Models;

public enum CategoryRemovalMode
{
    MoveProducts,
    ArchiveCategory,
    ArchiveProducts,
    DeleteProducts
}

public sealed record RemoveCategoryRequest(
    CategoryRemovalMode Mode,
    Guid? TargetCategoryId = null);

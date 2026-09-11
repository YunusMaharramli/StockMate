namespace StockMate.Application.Products.Models;

public enum BulkProductAction
{
    Activate,
    Deactivate,
    Archive,
    Restore,
    Move
}

public sealed record BulkProductActionRequest(
    IReadOnlyCollection<Guid> ProductIds,
    BulkProductAction Action,
    Guid? CategoryId = null,
    Guid? SubcategoryId = null);

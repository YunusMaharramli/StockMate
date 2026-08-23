using StockMate.Application.Products.Models;

namespace StockMate.Application.Products.Services;

public interface IProductService
{
    Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ProductDto>> GetActivePageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task ChangePriceAsync(
        Guid productId,
        decimal newPrice,
        CancellationToken cancellationToken = default);

    Task AddStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default);

    Task RemoveStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default);

    Task SetStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovementDto>> GetStockHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
    Task UpdateDetailsAsync(
    Guid productId,
    UpdateProductRequest request,
    CancellationToken cancellationToken = default);
    Task<PagedResult<ProductDto>> GetAdminPageAsync(
    int pageNumber,
    int pageSize,
    string? search,
    CancellationToken cancellationToken = default);
}
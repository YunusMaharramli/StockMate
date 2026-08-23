using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetActivePageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);

    Task AddStockMovementAsync(
        StockMovement stockMovement,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovement>> GetStockHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetByIdsAsync(
    IReadOnlyCollection<Guid> ids,
    CancellationToken cancellationToken = default);
    void Update(Product product);
    Task<IReadOnlyList<Product>> GetAdminPageAsync(
    int pageNumber,
    int pageSize,
    string? search,
    CancellationToken cancellationToken = default);

    Task<int> CountAdminProductsAsync(
        string? search,
        CancellationToken cancellationToken = default);
}

using StockMate.Domain.Entities;

using StockMate.Application.Products.Models;

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

    Task<IReadOnlyList<Product>> GetCatalogPageAsync(
        ProductCatalogQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountCatalogAsync(
        ProductCatalogQuery query,
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

    Task<IReadOnlyList<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetBySubcategoryAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task<bool> HasHistoricalReferencesAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    Task PrepareDeleteAsync(
        Product product,
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

    Task<IReadOnlyList<Product>> GetAdminPageAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken = default);

    Task<int> CountAdminProductsAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken = default);
}

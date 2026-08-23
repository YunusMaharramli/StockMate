using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly StockMateDbContext _dbContext;

    public ProductRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Id == id,
                cancellationToken);
    }

    public Task<Product?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return _dbContext.Products
            .FirstOrDefaultAsync(
                product => product.Code == normalizedCode,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return _dbContext.Products.AnyAsync(
            product => product.Code == normalizedCode,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetActivePageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.CountAsync(
            product => product.IsActive,
            cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Products.AddAsync(
            product,
            cancellationToken);
    }

    public async Task AddStockMovementAsync(
        StockMovement stockMovement,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StockMovements.AddAsync(
            stockMovement,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovement>> GetStockHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == productId)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
    IReadOnlyCollection<Guid> ids,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }
    public void Update(Product product)
    {
        _dbContext.Products.Update(product);
    }
    public async Task<IReadOnlyList<Product>> GetAdminPageAsync(
    int pageNumber,
    int pageSize,
    string? search,
    CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";

            query = query.Where(product =>
                EF.Functions.ILike(product.Name, searchTerm) ||
                EF.Functions.ILike(product.Code, searchTerm));
        }

        return await query
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminProductsAsync(
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";

            query = query.Where(product =>
                EF.Functions.ILike(product.Name, searchTerm) ||
                EF.Functions.ILike(product.Code, searchTerm));
        }

        return query.CountAsync(cancellationToken);
    }
}

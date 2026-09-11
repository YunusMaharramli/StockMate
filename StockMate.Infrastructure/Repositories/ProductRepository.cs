using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Application.Products.Models;
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
            .Include(product => product.Category)
            .Include(product => product.Subcategory)
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
            .Include(product => product.Category)
            .Include(product => product.Subcategory)
            .Where(product =>
                product.IsActive &&
                !product.IsArchived &&
                (product.CategoryId == null ||
                 (product.Category!.IsActive && !product.Category.IsArchived)) &&
                (product.SubcategoryId == null ||
                 (product.Subcategory!.IsActive && !product.Subcategory.IsArchived)))
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.CountAsync(
            product =>
                product.IsActive &&
                !product.IsArchived &&
                (product.CategoryId == null ||
                 (product.Category!.IsActive && !product.Category.IsArchived)) &&
                (product.SubcategoryId == null ||
                 (product.Subcategory!.IsActive && !product.Subcategory.IsArchived)),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetCatalogPageAsync(
        ProductCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildCatalogQuery(query)
            .OrderBy(product => product.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountCatalogAsync(
        ProductCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        return BuildCatalogQuery(query).CountAsync(cancellationToken);
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
            .Include(product => product.Category)
            .Include(product => product.Subcategory)
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

    public async Task<IReadOnlyList<Product>> GetAdminPageAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken = default)
    {
        return await BuildAdminQuery(query)
            .OrderBy(product => product.Name)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(product => product.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetBySubcategoryAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Where(product => product.SubcategoryId == subcategoryId)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAdminProductsAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken = default)
    {
        return BuildAdminQuery(query).CountAsync(cancellationToken);
    }

    private IQueryable<Product> BuildCatalogQuery(ProductCatalogQuery query)
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Subcategory)
            .Where(product =>
                product.IsActive &&
                !product.IsArchived &&
                (product.CategoryId == null ||
                 (product.Category!.IsActive && !product.Category.IsArchived)) &&
                (product.SubcategoryId == null ||
                 (product.Subcategory!.IsActive && !product.Subcategory.IsArchived)));

        if (query.CategoryId.HasValue)
            products = products.Where(product => product.CategoryId == query.CategoryId);

        if (!string.IsNullOrWhiteSpace(query.SubcategoryName))
        {
            var normalizedName = query.SubcategoryName.Trim().ToUpperInvariant();
            products = products.Where(product =>
                product.Subcategory != null &&
                product.Subcategory.NormalizedName == normalizedName);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Code, pattern));
        }

        return products;
    }

    private IQueryable<Product> BuildAdminQuery(AdminProductQuery query)
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Subcategory)
            .AsQueryable();

        if (!query.IncludeArchived)
            products = products.Where(product => !product.IsArchived);

        if (query.CategoryId.HasValue)
            products = products.Where(product => product.CategoryId == query.CategoryId);

        if (query.SubcategoryId.HasValue)
            products = products.Where(product => product.SubcategoryId == query.SubcategoryId);

        if (query.IsActive.HasValue)
            products = products.Where(product => product.IsActive == query.IsActive);

        if (query.InStock.HasValue)
        {
            products = query.InStock.Value
                ? products.Where(product => product.StockQuantity > 0)
                : products.Where(product => product.StockQuantity <= 0);
        }

        if (query.LowStock.HasValue)
        {
            products = query.LowStock.Value
                ? products.Where(product => product.StockQuantity <= product.MinimumStockQuantity)
                : products.Where(product => product.StockQuantity > product.MinimumStockQuantity);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            products = products.Where(product =>
                EF.Functions.ILike(product.Name, pattern) ||
                EF.Functions.ILike(product.Code, pattern));
        }

        return products;
    }
}

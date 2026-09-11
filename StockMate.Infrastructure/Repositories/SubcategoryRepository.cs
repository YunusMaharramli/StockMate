using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class SubcategoryRepository
    : ISubcategoryRepository
{
    private readonly StockMateDbContext _dbContext;

    public SubcategoryRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Subcategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Subcategories
            .FirstOrDefaultAsync(
                subcategory => subcategory.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Subcategory>> GetAllAsync(
        Guid? categoryId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Subcategories
            .AsNoTracking()
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(subcategory =>
                subcategory.CategoryId == categoryId.Value);
        }

        if (!includeArchived)
        {
            query = query.Where(subcategory =>
                !subcategory.IsArchived);
        }

        return await query
            .OrderBy(subcategory => subcategory.IsArchived)
            .ThenBy(subcategory => subcategory.SortOrder)
            .ThenBy(subcategory => subcategory.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subcategory>> GetActiveAsync(
        Guid? categoryId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Subcategories
            .AsNoTracking()
            .Where(subcategory =>
                subcategory.IsActive &&
                !subcategory.IsArchived &&
                _dbContext.Categories.Any(category =>
                    category.Id == subcategory.CategoryId &&
                    category.IsActive &&
                    !category.IsArchived));

        if (categoryId.HasValue)
        {
            query = query.Where(subcategory =>
                subcategory.CategoryId == categoryId.Value);
        }

        return await query
            .OrderBy(subcategory => subcategory.SortOrder)
            .ThenBy(subcategory => subcategory.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid categoryId,
        string normalizedName,
        Guid? excludedSubcategoryId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Subcategories.AnyAsync(
            subcategory =>
                subcategory.CategoryId == categoryId &&
                subcategory.NormalizedName == normalizedName &&
                (!excludedSubcategoryId.HasValue ||
                 subcategory.Id != excludedSubcategoryId.Value),
            cancellationToken);
    }

    public Task<int> CountProductsAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.CountAsync(
            product =>
                product.SubcategoryId == subcategoryId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Subcategory>> GetByCategoryTrackedAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Subcategories
            .Where(subcategory => subcategory.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Subcategory subcategory,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Subcategories.AddAsync(
            subcategory,
            cancellationToken);
    }

    public void Remove(Subcategory subcategory)
    {
        _dbContext.Subcategories.Remove(subcategory);
    }

    public void RemoveRange(IEnumerable<Subcategory> subcategories)
    {
        _dbContext.Subcategories.RemoveRange(subcategories);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly StockMateDbContext _dbContext;

    public CategoryRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories
            .FirstOrDefaultAsync(
                category => category.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Categories
            .AsNoTracking()
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(category =>
                !category.IsArchived);
        }

        return await query
            .OrderBy(category => category.IsArchived)
            .ThenBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(category =>
                category.IsActive &&
                !category.IsArchived)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Categories.AnyAsync(
            category =>
                category.NormalizedName == normalizedName &&
                (!excludedCategoryId.HasValue ||
                 category.Id != excludedCategoryId.Value),
            cancellationToken);
    }

    public Task<int> CountSubcategoriesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Subcategories.CountAsync(
            subcategory =>
                subcategory.CategoryId == categoryId,
            cancellationToken);
    }

    public Task<int> CountProductsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Products.CountAsync(
            product =>
                product.CategoryId == categoryId,
            cancellationToken);
    }

    public async Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(
            category,
            cancellationToken);
    }

    public void Remove(Category category)
    {
        _dbContext.Categories.Remove(category);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

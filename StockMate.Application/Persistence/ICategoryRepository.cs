using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountSubcategoriesAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<int> CountProductsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    void Remove(Category category);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
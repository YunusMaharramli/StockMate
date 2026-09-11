using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ISubcategoryRepository
{
    Task<Subcategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subcategory>> GetAllAsync(
        Guid? categoryId,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subcategory>> GetActiveAsync(
        Guid? categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid categoryId,
        string normalizedName,
        Guid? excludedSubcategoryId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountProductsAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subcategory>> GetByCategoryTrackedAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Subcategory subcategory,
        CancellationToken cancellationToken = default);

    void Remove(Subcategory subcategory);

    void RemoveRange(IEnumerable<Subcategory> subcategories);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

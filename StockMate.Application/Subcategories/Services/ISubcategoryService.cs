using StockMate.Application.Subcategories.Models;

namespace StockMate.Application.Subcategories.Services;

public interface ISubcategoryService
{
    Task<SubcategoryDto> CreateAsync(
        CreateSubcategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<SubcategoryDto?> GetByIdAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubcategoryDto>> GetAllAsync(
        Guid? categoryId = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubcategoryDto>> GetActiveAsync(
        Guid? categoryId = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid subcategoryId,
        UpdateSubcategoryRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid subcategoryId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task DeleteEmptyAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid subcategoryId,
        RemoveSubcategoryRequest request,
        CancellationToken cancellationToken = default);
}

using StockMate.Application.Categories.Models;

namespace StockMate.Application.Categories.Services;

public interface ICategoryService
{
    Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task DeleteEmptyAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid categoryId,
        RemoveCategoryRequest request,
        CancellationToken cancellationToken = default);
}

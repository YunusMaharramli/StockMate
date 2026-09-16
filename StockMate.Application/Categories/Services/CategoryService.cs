using StockMate.Application.Categories.Models;
using StockMate.Application.Persistence;
using CategoryEntity = StockMate.Domain.Entities.Category;

namespace StockMate.Application.Categories.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubcategoryRepository _subcategoryRepository;
    private readonly IProductRepository _productRepository;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ISubcategoryRepository subcategoryRepository,
        IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _subcategoryRepository = subcategoryRepository;
        _productRepository = productRepository;
    }

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedName = NormalizeName(request.Name);

        var nameExists = await _categoryRepository.NameExistsAsync(
            normalizedName,
            cancellationToken: cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"Категория с названием '{request.Name.Trim()}' уже существует.");
        }

        var category = new CategoryEntity(
            request.Name,
            request.SortOrder);

        await _categoryRepository.AddAsync(
            category,
            cancellationToken);

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);

        return await MapAsync(category, cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            return null;
        }

        return await MapAsync(category, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(
            includeArchived,
            cancellationToken);

        var result = new List<CategoryDto>(categories.Count);

        foreach (var category in categories)
        {
            result.Add(
                await MapAsync(category, cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetActiveAsync(
            cancellationToken);

        var result = new List<CategoryDto>(categories.Count);

        foreach (var category in categories)
        {
            result.Add(
                await MapAsync(category, cancellationToken));
        }

        return result;
    }

    public async Task UpdateAsync(
        Guid categoryId,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await GetRequiredCategoryAsync(
            categoryId,
            cancellationToken);

        var normalizedName = NormalizeName(request.Name);

        var nameExists = await _categoryRepository.NameExistsAsync(
            normalizedName,
            categoryId,
            cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"Другая категория с названием '{request.Name.Trim()}' уже существует.");
        }

        category.Rename(request.Name);
        category.ChangeSortOrder(request.SortOrder);

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredCategoryAsync(
            categoryId,
            cancellationToken);

        if (isActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ArchiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredCategoryAsync(
            categoryId,
            cancellationToken);

        category.Archive();

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RestoreAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredCategoryAsync(
            categoryId,
            cancellationToken);

        category.Restore();

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteEmptyAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await GetRequiredCategoryAsync(
            categoryId,
            cancellationToken);

        var subcategoryCount =
            await _categoryRepository.CountSubcategoriesAsync(
                categoryId,
                cancellationToken);

        var productCount =
            await _categoryRepository.CountProductsAsync(
                categoryId,
                cancellationToken);

        if (subcategoryCount > 0 || productCount > 0)
        {
            throw new InvalidOperationException(
                "Категория не пуста. Переместите или архивируйте её товары и подкатегории перед удалением.");
        }

        _categoryRepository.Remove(category);

        await _categoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RemoveAsync(
        Guid categoryId,
        RemoveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await GetRequiredCategoryAsync(categoryId, cancellationToken);
        var products = await _productRepository.GetByCategoryAsync(categoryId, cancellationToken);
        var subcategories = await _subcategoryRepository.GetByCategoryTrackedAsync(
            categoryId,
            cancellationToken);

        switch (request.Mode)
        {
            case CategoryRemovalMode.MoveProducts:
            {
                if (!request.TargetCategoryId.HasValue ||
                    request.TargetCategoryId.Value == categoryId)
                {
                    throw new ArgumentException(
                        "Нужно выбрать другую целевую категорию.",
                        nameof(request));
                }

                var target = await GetRequiredCategoryAsync(
                    request.TargetCategoryId.Value,
                    cancellationToken);

                if (target.IsArchived)
                    throw new InvalidOperationException("Товары нельзя перемещать в архивную категорию.");

                foreach (var product in products)
                    product.ChangeClassification(target.Id, null);

                _subcategoryRepository.RemoveRange(subcategories);
                _categoryRepository.Remove(category);
                break;
            }

            case CategoryRemovalMode.ArchiveCategory:
                category.Archive();
                break;

            case CategoryRemovalMode.ArchiveProducts:
                foreach (var product in products)
                    product.Archive();

                foreach (var subcategory in subcategories)
                    subcategory.Archive();

                category.Archive();
                break;

            case CategoryRemovalMode.DeleteProducts:
                foreach (var product in products)
                {
                    if (await _productRepository.HasHistoricalReferencesAsync(
                            product.Id,
                            cancellationToken))
                    {
                        throw new InvalidOperationException(
                            $"'{product.Name}' используется в истории заказов или закупок. Архивируйте категорию вместе с товарами.");
                    }
                }

                foreach (var product in products)
                    await _productRepository.PrepareDeleteAsync(product, cancellationToken);

                _subcategoryRepository.RemoveRange(subcategories);
                _categoryRepository.Remove(category);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request));
        }

        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CategoryEntity> GetRequiredCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await _categoryRepository.GetByIdAsync(
                   categoryId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Категория не найдена.");
    }

    private async Task<CategoryDto> MapAsync(
        CategoryEntity category,
        CancellationToken cancellationToken)
    {
        var subcategoryCount =
            await _categoryRepository.CountSubcategoriesAsync(
                category.Id,
                cancellationToken);

        var productCount =
            await _categoryRepository.CountProductsAsync(
                category.Id,
                cancellationToken);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.IsActive,
            category.IsArchived,
            category.SortOrder,
            subcategoryCount,
            productCount);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Название категории обязательно.",
                nameof(name));
        }

        return name.Trim().ToUpperInvariant();
    }
}

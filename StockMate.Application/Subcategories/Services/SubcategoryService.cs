using StockMate.Application.Persistence;
using StockMate.Application.Subcategories.Models;
using SubcategoryEntity = StockMate.Domain.Entities.Subcategory;

namespace StockMate.Application.Subcategories.Services;

public sealed class SubcategoryService : ISubcategoryService
{
    private readonly ISubcategoryRepository _subcategoryRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public SubcategoryService(
        ISubcategoryRepository subcategoryRepository,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository)
    {
        _subcategoryRepository = subcategoryRepository;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    public async Task<SubcategoryDto> CreateAsync(
        CreateSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(
                           request.CategoryId,
                           cancellationToken)
                       ?? throw new KeyNotFoundException(
                           "Категория не найдена.");

        if (category.IsArchived)
        {
            throw new InvalidOperationException(
                "Нельзя создать подкатегорию в архивной категории.");
        }

        var normalizedName = NormalizeName(request.Name);

        var nameExists = await _subcategoryRepository.NameExistsAsync(
            request.CategoryId,
            normalizedName,
            cancellationToken: cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"Подкатегория с названием '{request.Name.Trim()}' уже существует в этой категории.");
        }

        var subcategory = new SubcategoryEntity(
            request.CategoryId,
            request.Name,
            request.SortOrder);

        await _subcategoryRepository.AddAsync(
            subcategory,
            cancellationToken);

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);

        return await MapAsync(
            subcategory,
            category.Name,
            cancellationToken);
    }

    public async Task<SubcategoryDto?> GetByIdAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var subcategory = await _subcategoryRepository.GetByIdAsync(
            subcategoryId,
            cancellationToken);

        if (subcategory is null)
        {
            return null;
        }

        return await MapAsync(
            subcategory,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SubcategoryDto>> GetAllAsync(
        Guid? categoryId = null,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var subcategories = await _subcategoryRepository.GetAllAsync(
            categoryId,
            includeArchived,
            cancellationToken);

        var result = new List<SubcategoryDto>(
            subcategories.Count);

        foreach (var subcategory in subcategories)
        {
            result.Add(
                await MapAsync(
                    subcategory,
                    cancellationToken));
        }

        return result;
    }

    public async Task<IReadOnlyList<SubcategoryDto>> GetActiveAsync(
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var subcategories = await _subcategoryRepository.GetActiveAsync(
            categoryId,
            cancellationToken);

        var result = new List<SubcategoryDto>(
            subcategories.Count);

        foreach (var subcategory in subcategories)
        {
            result.Add(
                await MapAsync(
                    subcategory,
                    cancellationToken));
        }

        return result;
    }

    public async Task UpdateAsync(
        Guid subcategoryId,
        UpdateSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        var normalizedName = NormalizeName(request.Name);

        var nameExists = await _subcategoryRepository.NameExistsAsync(
            subcategory.CategoryId,
            normalizedName,
            subcategoryId,
            cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException(
                $"Другая подкатегория с названием '{request.Name.Trim()}' уже существует в этой категории.");
        }

        subcategory.Rename(request.Name);
        subcategory.ChangeSortOrder(request.SortOrder);

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task SetActiveAsync(
        Guid subcategoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        if (isActive)
        {
            var category = await _categoryRepository.GetByIdAsync(
                               subcategory.CategoryId,
                               cancellationToken)
                           ?? throw new KeyNotFoundException(
                               "Категория не найдена.");

            if (category.IsArchived)
            {
                throw new InvalidOperationException(
                    "Подкатегорию в архивной категории нельзя активировать.");
            }

            subcategory.Activate();
        }
        else
        {
            subcategory.Deactivate();
        }

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ArchiveAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        subcategory.Archive();

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RestoreAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(
                           subcategory.CategoryId,
                           cancellationToken)
                       ?? throw new KeyNotFoundException(
                           "Категория не найдена.");

        if (category.IsArchived)
        {
            throw new InvalidOperationException(
                "Restore the parent category first.");
        }

        subcategory.Restore();

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task DeleteEmptyAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        var productCount =
            await _subcategoryRepository.CountProductsAsync(
                subcategoryId,
                cancellationToken);

        if (productCount > 0)
        {
            throw new InvalidOperationException(
                "The subcategory contains products. Move or archive them before deleting it.");
        }

        _subcategoryRepository.Remove(subcategory);

        await _subcategoryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RemoveAsync(
        Guid subcategoryId,
        RemoveSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var subcategory = await GetRequiredSubcategoryAsync(
            subcategoryId,
            cancellationToken);

        var products = await _productRepository.GetBySubcategoryAsync(
            subcategoryId,
            cancellationToken);

        switch (request.Mode)
        {
            case SubcategoryRemovalMode.MoveProducts:
            {
                if (!request.TargetSubcategoryId.HasValue ||
                    request.TargetSubcategoryId.Value == subcategoryId)
                {
                    throw new ArgumentException(
                        "Нужно выбрать другую целевую подкатегорию.",
                        nameof(request));
                }

                var target = await GetRequiredSubcategoryAsync(
                    request.TargetSubcategoryId.Value,
                    cancellationToken);

                if (target.IsArchived)
                    throw new InvalidOperationException("Товары нельзя перемещать в архивную подкатегорию.");

                var targetCategory = await _categoryRepository.GetByIdAsync(
                    target.CategoryId,
                    cancellationToken)
                    ?? throw new KeyNotFoundException("Целевая категория не найдена.");

                if (targetCategory.IsArchived)
                    throw new InvalidOperationException("Товары нельзя перемещать в архивную категорию.");

                foreach (var product in products)
                    product.ChangeClassification(target.CategoryId, target.Id);

                _subcategoryRepository.Remove(subcategory);
                break;
            }

            case SubcategoryRemovalMode.ArchiveSubcategory:
                subcategory.Archive();
                break;

            case SubcategoryRemovalMode.ArchiveProducts:
                foreach (var product in products)
                    product.Archive();

                subcategory.Archive();
                break;

            case SubcategoryRemovalMode.DeleteProducts:
                foreach (var product in products)
                {
                    if (await _productRepository.HasHistoricalReferencesAsync(
                            product.Id,
                            cancellationToken))
                    {
                        throw new InvalidOperationException(
                            $"'{product.Name}' используется в истории заказов или закупок. Архивируйте подкатегорию вместе с товарами.");
                    }
                }

                foreach (var product in products)
                    await _productRepository.PrepareDeleteAsync(product, cancellationToken);

                _subcategoryRepository.Remove(subcategory);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(request));
        }

        await _subcategoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<SubcategoryEntity> GetRequiredSubcategoryAsync(
        Guid subcategoryId,
        CancellationToken cancellationToken)
    {
        return await _subcategoryRepository.GetByIdAsync(
                   subcategoryId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Подкатегория не найдена.");
    }

    private async Task<SubcategoryDto> MapAsync(
        SubcategoryEntity subcategory,
        CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(
                           subcategory.CategoryId,
                           cancellationToken)
                       ?? throw new KeyNotFoundException(
                           "Родительская категория подкатегории не найдена.");

        return await MapAsync(
            subcategory,
            category.Name,
            cancellationToken);
    }

    private async Task<SubcategoryDto> MapAsync(
        SubcategoryEntity subcategory,
        string categoryName,
        CancellationToken cancellationToken)
    {
        var productCount =
            await _subcategoryRepository.CountProductsAsync(
                subcategory.Id,
                cancellationToken);

        return new SubcategoryDto(
            subcategory.Id,
            subcategory.CategoryId,
            categoryName,
            subcategory.Name,
            subcategory.IsActive,
            subcategory.IsArchived,
            subcategory.SortOrder,
            productCount);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Название подкатегории обязательно.",
                nameof(name));
        }

        return name.Trim().ToUpperInvariant();
    }
}

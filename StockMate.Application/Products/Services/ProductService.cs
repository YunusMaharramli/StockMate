using StockMate.Application.Persistence;
using StockMate.Application.Products.Models;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Products.Services;

public class ProductService : IProductService
{
    private static readonly int[] AllowedPageSizes = [5, 10, 20];

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository? _categoryRepository;
    private readonly ISubcategoryRepository? _subcategoryRepository;

    public ProductService(
        IProductRepository productRepository,
        ICategoryRepository? categoryRepository = null,
        ISubcategoryRepository? subcategoryRepository = null)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _subcategoryRepository = subcategoryRepository;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CategoryId.HasValue)
        {
            await ValidateClassificationAsync(
                request.CategoryId.Value,
                request.SubcategoryId,
                cancellationToken);
        }

        if (await _productRepository.CodeExistsAsync(
                request.Code,
                cancellationToken))
        {
            throw new InvalidOperationException(
                $"Product code '{request.Code}' already exists.");
        }

        var product = new Product(
            request.Name,
            request.Code,
            request.Price,
            request.InitialStockQuantity,
            request.Description,
            request.ImageReference,
            request.CategoryId,
            request.SubcategoryId,
            request.MinimumStockQuantity);

        var initialMovement = new StockMovement(
            product.Id,
            StockMovementType.InitialStock,
            previousQuantity: 0,
            newQuantity: product.StockQuantity,
            reason: "Initial product stock.");

        await _productRepository.AddAsync(
            product,
            cancellationToken);

        await _productRepository.AddStockMovementAsync(
            initialMovement,
            cancellationToken);

        await _productRepository.SaveChangesAsync(cancellationToken);

        var savedProduct = await _productRepository.GetByIdAsync(
            product.Id,
            cancellationToken);

        return MapProduct(savedProduct ?? product);
    }

    public async Task<ProductDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        return product is null
            ? null
            : MapProduct(product);
    }

    public async Task<PagedResult<ProductDto>> GetActivePageAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        if (!AllowedPageSizes.Contains(pageSize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be 5, 10 or 20.");
        }

        var products = await _productRepository.GetActivePageAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        var totalItems = await _productRepository.CountActiveAsync(
            cancellationToken);

        return new PagedResult<ProductDto>(
            products.Select(MapProduct).ToList(),
            pageNumber,
            pageSize,
            totalItems);
    }

    public async Task<PagedResult<ProductCatalogItemDto>> GetCatalogPageAsync(
        ProductCatalogQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.PageNumber, query.PageSize);

        var products = await _productRepository.GetCatalogPageAsync(
            query,
            cancellationToken);

        var totalItems = await _productRepository.CountCatalogAsync(
            query,
            cancellationToken);

        return new PagedResult<ProductCatalogItemDto>(
            products.Select(MapCatalogProduct).ToList(),
            query.PageNumber,
            query.PageSize,
            totalItems);
    }

    public async Task<ProductCatalogItemDto?> GetCatalogProductByIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(
            productId,
            cancellationToken);

        return product is null || !product.IsCatalogVisible
            ? null
            : MapCatalogProduct(product);
    }

    public async Task ChangePriceAsync(
        Guid productId,
        decimal newPrice,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        product.ChangePrice(newPrice);

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        var movement = product.AddStock(quantity, reason);

        await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task RemoveStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        var movement = product.RemoveStock(quantity, reason);

        await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task SetStockAsync(
        Guid productId,
        int quantity,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        var movement = product.SetStock(quantity, reason);

        await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task ActivateAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        product.Activate();

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        product.Deactivate();

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockMovementDto>>
        GetStockHistoryAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
    {
        await GetRequiredProductAsync(productId, cancellationToken);

        var movements =
            await _productRepository.GetStockHistoryAsync(
                productId,
                cancellationToken);

        return movements
            .Select(movement => new StockMovementDto(
                movement.Id,
                movement.Type,
                movement.PreviousQuantity,
                movement.NewQuantity,
                movement.QuantityChange,
                movement.Reason,
                movement.OrderId,
                movement.CreatedAtUtc))
            .ToList();
    }

    private async Task<Product> GetRequiredProductAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        return await _productRepository.GetByIdAsync(
                   productId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Product '{productId}' was not found.");
    }

    private async Task SaveMovementAsync(
        StockMovement movement,
        CancellationToken cancellationToken)
    {
        await _productRepository.AddStockMovementAsync(
            movement,
            cancellationToken);

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    private static ProductDto MapProduct(Product product)
    {
        return new ProductDto(
            product.Id,
            product.Name,
            product.Code,
            product.Price,
            product.StockQuantity,
            product.Description,
            product.ImageReference,
            product.IsActive,
            product.IsArchived,
            product.CategoryId,
            product.Category?.Name,
            product.SubcategoryId,
            product.Subcategory?.Name,
            product.MinimumStockQuantity,
            product.StockQuantity <= product.MinimumStockQuantity,
            product.IsCatalogVisible);
    }

    private static ProductCatalogItemDto MapCatalogProduct(Product product)
    {
        return new ProductCatalogItemDto(
            product.Id,
            product.Name,
            product.Code,
            product.Price,
            product.Description,
            product.ImageReference,
            product.CategoryId,
            product.Category?.Name,
            product.SubcategoryId,
            product.Subcategory?.Name);
    }

    public async Task UpdateDetailsAsync(
    Guid productId,
    UpdateProductRequest request,
    CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(
            productId,
            cancellationToken);

        product.UpdateDetails(
            request.Name,
            request.Description,
            request.ImageReference);

        if (request.UpdateClassification)
        {
            if (!request.CategoryId.HasValue)
                throw new ArgumentException("Category ID is required.", nameof(request));

            await ValidateClassificationAsync(
                request.CategoryId.Value,
                request.SubcategoryId,
                cancellationToken);

            product.ChangeClassification(
                request.CategoryId.Value,
                request.SubcategoryId);
        }

        if (request.MinimumStockQuantity.HasValue)
            product.SetMinimumStockQuantity(request.MinimumStockQuantity.Value);

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<ProductDto>> GetAdminPageAsync(
    int pageNumber,
    int pageSize,
    string? search,
    CancellationToken cancellationToken = default)
    {
        if (pageNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        if (!AllowedPageSizes.Contains(pageSize))
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be 5, 10 or 20.");
        }

        var products = await _productRepository.GetAdminPageAsync(
            pageNumber,
            pageSize,
            search,
            cancellationToken);

        var totalItems =
            await _productRepository.CountAdminProductsAsync(
                search,
                cancellationToken);

        return new PagedResult<ProductDto>(
            products.Select(MapProduct).ToList(),
            pageNumber,
            pageSize,
            totalItems);
    }

    public async Task<PagedResult<ProductDto>> GetAdminPageAsync(
        AdminProductQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePage(query.PageNumber, query.PageSize);

        var products = await _productRepository.GetAdminPageAsync(
            query,
            cancellationToken);

        var totalItems = await _productRepository.CountAdminProductsAsync(
            query,
            cancellationToken);

        return new PagedResult<ProductDto>(
            products.Select(MapProduct).ToList(),
            query.PageNumber,
            query.PageSize,
            totalItems);
    }

    public async Task ArchiveAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(productId, cancellationToken);
        product.Archive();
        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(productId, cancellationToken);
        product.Restore();
        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var product = await GetRequiredProductAsync(productId, cancellationToken);

        if (await _productRepository.HasHistoricalReferencesAsync(
                productId,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Bu məhsul sifariş və ya alış tarixçəsində istifadə olunub, ona görə həmişəlik silinə bilməz. Məhsulu arxivləyin.");
        }

        await _productRepository.PrepareDeleteAsync(product, cancellationToken);
        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyBulkActionAsync(
        BulkProductActionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ids = request.ProductIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
            throw new ArgumentException("At least one product must be selected.", nameof(request));

        if (ids.Length > 500)
            throw new ArgumentException("No more than 500 products can be changed at once.", nameof(request));

        if (request.Action == BulkProductAction.Move)
        {
            if (!request.CategoryId.HasValue)
                throw new ArgumentException("A target category is required.", nameof(request));

            await ValidateClassificationAsync(
                request.CategoryId.Value,
                request.SubcategoryId,
                cancellationToken);
        }

        var products = await _productRepository.GetByIdsAsync(ids, cancellationToken);
        if (products.Count != ids.Length)
            throw new KeyNotFoundException("One or more selected products were not found.");

        foreach (var product in products)
        {
            switch (request.Action)
            {
                case BulkProductAction.Activate:
                    product.Activate();
                    break;
                case BulkProductAction.Deactivate:
                    product.Deactivate();
                    break;
                case BulkProductAction.Archive:
                    product.Archive();
                    break;
                case BulkProductAction.Restore:
                    product.Restore();
                    break;
                case BulkProductAction.Move:
                    product.ChangeClassification(
                        request.CategoryId!.Value,
                        request.SubcategoryId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        await _productRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateClassificationAsync(
        Guid categoryId,
        Guid? subcategoryId,
        CancellationToken cancellationToken)
    {
        if (_categoryRepository is null || _subcategoryRepository is null)
            throw new InvalidOperationException("Catalog repositories are not configured.");

        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new KeyNotFoundException("Category was not found.");

        if (category.IsArchived)
            throw new InvalidOperationException("An archived category cannot be assigned to a product.");

        if (!subcategoryId.HasValue)
            return;

        var subcategory = await _subcategoryRepository.GetByIdAsync(
            subcategoryId.Value,
            cancellationToken)
            ?? throw new KeyNotFoundException("Subcategory was not found.");

        if (subcategory.CategoryId != categoryId)
            throw new InvalidOperationException("The subcategory does not belong to the selected category.");

        if (subcategory.IsArchived)
            throw new InvalidOperationException("An archived subcategory cannot be assigned to a product.");
    }

    private static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        if (!AllowedPageSizes.Contains(pageSize))
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be 5, 10 or 20.");
    }
}

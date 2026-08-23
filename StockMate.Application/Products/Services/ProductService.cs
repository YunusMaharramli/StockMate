using StockMate.Application.Persistence;
using StockMate.Application.Products.Models;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Products.Services;

public class ProductService : IProductService
{
    private static readonly int[] AllowedPageSizes = [5, 10, 20];

    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
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
            request.ImageReference);

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

        return MapProduct(product);
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
            product.IsActive);
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
}

using StockMate.Application.Persistence;
using StockMate.Application.StockPurchases.Models;
using StockMate.Domain.Entities;

namespace StockMate.Application.StockPurchases.Services;

public sealed class StockPurchaseService
    : IStockPurchaseService
{
    private readonly IStockPurchaseRepository
        _stockPurchaseRepository;

    private readonly IProductRepository
        _productRepository;

    public StockPurchaseService(
        IStockPurchaseRepository stockPurchaseRepository,
        IProductRepository productRepository)
    {
        _stockPurchaseRepository = stockPurchaseRepository;
        _productRepository = productRepository;
    }

    public async Task<StockPurchaseDto> CreateAsync(
        CreateStockPurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null ||
            request.Items.Count == 0)
        {
            throw new InvalidOperationException(
                "Закупка должна содержать хотя бы один товар.");
        }

        var productIds = request.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToArray();

        if (productIds.Length != request.Items.Count)
        {
            throw new InvalidOperationException(
                "Один и тот же товар нельзя добавлять более одного раза.");
        }

        var products =
            await _productRepository.GetByIdsAsync(
                productIds,
                cancellationToken);

        var productsById = products.ToDictionary(
            product => product.Id);

        var purchase = new StockPurchase(
            request.PurchasedAtUtc,
            request.Note);

        foreach (var requestedItem in request.Items)
        {
            if (!productsById.TryGetValue(
                    requestedItem.ProductId,
                    out var product))
            {
                throw new KeyNotFoundException(
                    $"Товар '{requestedItem.ProductId}' не найден.");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Неактивный товар '{product.Name}' нельзя закупить.");
            }

            purchase.AddItem(
                product,
                requestedItem.Quantity,
                requestedItem.UnitCost);

            var stockMovement = product.AddStock(
                requestedItem.Quantity,
                $"Stock purchase {purchase.Id}");

            _productRepository.Update(product);

            await _productRepository.AddStockMovementAsync(
                stockMovement,
                cancellationToken);
        }

        await _stockPurchaseRepository.AddAsync(
            purchase,
            cancellationToken);

        await _stockPurchaseRepository.SaveChangesAsync(
            cancellationToken);

        return MapPurchase(purchase);
    }

    public async Task<StockPurchaseDto?> GetByIdAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        var purchase =
            await _stockPurchaseRepository.GetByIdAsync(
                purchaseId,
                cancellationToken);

        return purchase is null
            ? null
            : MapPurchase(purchase);
    }

    public async Task<IReadOnlyList<StockPurchaseDto>>
        GetByPeriodAsync(
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
    {
        var normalizedFrom =
            NormalizeUtc(fromUtc);

        var normalizedTo =
            NormalizeUtc(toUtc);

        if (normalizedFrom >= normalizedTo)
        {
            throw new ArgumentException(
                "Дата начала должна быть раньше даты окончания.");
        }

        var purchases =
            await _stockPurchaseRepository.GetByPeriodAsync(
                normalizedFrom,
                normalizedTo,
                cancellationToken);

        return purchases
            .Select(MapPurchase)
            .ToList();
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }

    private static StockPurchaseDto MapPurchase(
        StockPurchase purchase)
    {
        var items = purchase.Items
            .Select(item => new StockPurchaseItemDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.ProductCode,
                item.Quantity,
                item.UnitCost,
                item.LineTotal))
            .ToList();

        return new StockPurchaseDto(
            purchase.Id,
            purchase.PurchasedAtUtc,
            purchase.Note,
            items,
            purchase.TotalAmount,
            purchase.CreatedAtUtc);
    }
}

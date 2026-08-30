using StockMate.Application.Persistence;
using StockMate.Application.Reports.Models;

namespace StockMate.Application.Reports.Services;

public sealed class InventoryReportService
    : IInventoryReportService
{
    private readonly IStockPurchaseRepository
        _stockPurchaseRepository;

    private readonly ISaleRecordRepository
        _saleRecordRepository;

    private readonly IDeliveryCompanyRepository
        _deliveryCompanyRepository;

    public InventoryReportService(
        IStockPurchaseRepository stockPurchaseRepository,
        ISaleRecordRepository saleRecordRepository,
        IDeliveryCompanyRepository deliveryCompanyRepository)
    {
        _stockPurchaseRepository = stockPurchaseRepository;
        _saleRecordRepository = saleRecordRepository;
        _deliveryCompanyRepository = deliveryCompanyRepository;
    }

    public async Task<InventoryTurnoverReportDto> GetTurnoverAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? deliveryCompanyId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFrom = NormalizeUtc(fromUtc);
        var normalizedTo = NormalizeUtc(toUtc);

        if (normalizedFrom >= normalizedTo)
        {
            throw new ArgumentException(
                "The start date must be earlier than the end date.");
        }

        string? deliveryCompanyName = null;

        if (deliveryCompanyId.HasValue)
        {
            var company =
                await _deliveryCompanyRepository.GetByIdAsync(
                    deliveryCompanyId.Value,
                    cancellationToken)
                ?? throw new KeyNotFoundException(
                    "Delivery company was not found.");

            deliveryCompanyName = company.Name;
        }

        var purchases =
            await _stockPurchaseRepository.GetByPeriodAsync(
                normalizedFrom,
                normalizedTo,
                cancellationToken);

        var sales =
            await _saleRecordRepository.GetByPeriodAsync(
                normalizedFrom,
                normalizedTo,
                deliveryCompanyId,
                cancellationToken);

        var purchasedByProduct = purchases
            .SelectMany(purchase => purchase.Items)
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Name = group.First().ProductName,
                    Code = group.First().ProductCode,
                    Quantity = group.Sum(item => item.Quantity),
                    Amount = group.Sum(item => item.LineTotal)
                });

        var soldByProduct = sales
            .SelectMany(sale => sale.Items)
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Name = group.First().ProductName,
                    Code = group.First().ProductCode,
                    Quantity = group.Sum(item => item.Quantity),
                    Amount = group.Sum(item => item.LineTotal)
                });

        var productIds = purchasedByProduct.Keys
            .Union(soldByProduct.Keys)
            .ToArray();

        var products = productIds
            .Select(productId =>
            {
                purchasedByProduct.TryGetValue(
                    productId,
                    out var purchased);

                soldByProduct.TryGetValue(
                    productId,
                    out var sold);

                var purchasedQuantity =
                    purchased?.Quantity ?? 0;

                var purchaseAmount =
                    purchased?.Amount ?? 0;

                var soldQuantity =
                    sold?.Quantity ?? 0;

                var salesAmount =
                    sold?.Amount ?? 0;

                return new ProductTurnoverDto(
                    productId,
                    purchased?.Name ?? sold!.Name,
                    purchased?.Code ?? sold!.Code,
                    purchasedQuantity,
                    CalculateAverage(
                        purchaseAmount,
                        purchasedQuantity),
                    purchaseAmount,
                    soldQuantity,
                    CalculateAverage(
                        salesAmount,
                        soldQuantity),
                    salesAmount);
            })
            .OrderBy(product => product.ProductName)
            .ToList();

        return new InventoryTurnoverReportDto(
            normalizedFrom,
            normalizedTo,
            deliveryCompanyId,
            deliveryCompanyName,
            products,
            products.Sum(product =>
                product.PurchasedQuantity),
            products.Sum(product =>
                product.PurchaseAmount),
            products.Sum(product =>
                product.SoldQuantity),
            products.Sum(product =>
                product.SalesAmount));
    }

    private static decimal CalculateAverage(
        decimal amount,
        int quantity)
    {
        return quantity == 0
            ? 0
            : decimal.Round(
                amount / quantity,
                2,
                MidpointRounding.AwayFromZero);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
    }
}
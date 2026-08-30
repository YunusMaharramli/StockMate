using StockMate.Application.StockPurchases.Models;

namespace StockMate.Application.StockPurchases.Services;

public interface IStockPurchaseService
{
    Task<StockPurchaseDto> CreateAsync(
        CreateStockPurchaseRequest request,
        CancellationToken cancellationToken = default);

    Task<StockPurchaseDto?> GetByIdAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockPurchaseDto>> GetByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
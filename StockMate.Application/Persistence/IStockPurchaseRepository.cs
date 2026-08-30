using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface IStockPurchaseRepository
{
    Task<StockPurchase?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockPurchase>> GetByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StockPurchase purchase,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
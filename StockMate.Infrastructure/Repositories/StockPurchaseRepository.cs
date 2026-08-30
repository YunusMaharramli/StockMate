using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class StockPurchaseRepository
    : IStockPurchaseRepository
{
    private readonly StockMateDbContext _dbContext;

    public StockPurchaseRepository(
        StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StockPurchase?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.StockPurchases
            .AsNoTracking()
            .Include(purchase => purchase.Items)
            .FirstOrDefaultAsync(
                purchase => purchase.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<StockPurchase>> GetByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockPurchases
            .AsNoTracking()
            .Include(purchase => purchase.Items)
            .Where(purchase =>
                purchase.PurchasedAtUtc >= fromUtc &&
                purchase.PurchasedAtUtc < toUtc)
            .OrderByDescending(
                purchase => purchase.PurchasedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        StockPurchase purchase,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.StockPurchases.AddAsync(
            purchase,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
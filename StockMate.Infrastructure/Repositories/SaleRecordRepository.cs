using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class SaleRecordRepository
    : ISaleRecordRepository
{
    private readonly StockMateDbContext _dbContext;

    public SaleRecordRepository(
        StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SaleRecord>> GetByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? deliveryCompanyId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SaleRecords
            .AsNoTracking()
            .Include(sale => sale.Items)
            .Where(sale =>
                sale.SoldAtUtc >= fromUtc &&
                sale.SoldAtUtc < toUtc);

        if (deliveryCompanyId.HasValue)
        {
            query = query.Where(sale =>
                sale.DeliveryCompanyId ==
                deliveryCompanyId.Value);
        }

        return await query
            .OrderByDescending(sale => sale.SoldAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaleRecords.AnyAsync(
            sale => sale.ShipmentId == shipmentId,
            cancellationToken);
    }

    public async Task AddAsync(
        SaleRecord saleRecord,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaleRecords.AddAsync(
            saleRecord,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
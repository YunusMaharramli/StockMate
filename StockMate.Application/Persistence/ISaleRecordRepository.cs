using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ISaleRecordRepository
{
    Task<IReadOnlyList<SaleRecord>> GetByPeriodAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? deliveryCompanyId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForShipmentAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SaleRecord saleRecord,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
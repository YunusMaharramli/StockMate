using StockMate.Domain.Entities;

namespace StockMate.Application.Sales.Services;

public interface ISaleRecordingService
{
    Task AddAsync(
        Order order,
        IReadOnlyDictionary<Guid, int> deliveredQuantities,
        DateTime soldAtUtc,
        Guid? shipmentId = null,
        CancellationToken cancellationToken = default);
}
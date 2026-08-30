using StockMate.Application.Persistence;
using StockMate.Domain.Entities;

namespace StockMate.Application.Sales.Services;

public sealed class SaleRecordingService
    : ISaleRecordingService
{
    private readonly ISaleRecordRepository
        _saleRecordRepository;

    public SaleRecordingService(
        ISaleRecordRepository saleRecordRepository)
    {
        _saleRecordRepository = saleRecordRepository;
    }

    public async Task AddAsync(
        Order order,
        IReadOnlyDictionary<Guid, int> deliveredQuantities,
        DateTime soldAtUtc,
        Guid? shipmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (deliveredQuantities.Count == 0)
        {
            throw new InvalidOperationException(
                "A sale record must contain at least one product.");
        }

        if (shipmentId.HasValue &&
            await _saleRecordRepository.ExistsForShipmentAsync(
                shipmentId.Value,
                cancellationToken))
        {
            throw new InvalidOperationException(
                "A sale record already exists for this shipment.");
        }

        var orderItems = order.Items.ToDictionary(
            item => item.Id);

        var saleRecord = new SaleRecord(
            order,
            soldAtUtc,
            shipmentId);

        foreach (var deliveredItem in deliveredQuantities)
        {
            if (!orderItems.TryGetValue(
                    deliveredItem.Key,
                    out var orderItem))
            {
                throw new KeyNotFoundException(
                    $"Order item '{deliveredItem.Key}' was not found.");
            }

            saleRecord.AddItem(
                orderItem,
                deliveredItem.Value);
        }

        await _saleRecordRepository.AddAsync(
            saleRecord,
            cancellationToken);
    }
}
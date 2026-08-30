using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Persistence;

public interface IShipmentRepository
{
    Task<OrderShipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderShipment>> GetByStaffIdAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderShipment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderShipmentItem>> GetItemsAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShipmentStatusHistory>> GetHistoryAsync(Guid shipmentId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderShipment shipment, CancellationToken cancellationToken = default);
    Task AddItemAsync(OrderShipmentItem item, CancellationToken cancellationToken = default);
    Task AddHistoryAsync(ShipmentStatusHistory history, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

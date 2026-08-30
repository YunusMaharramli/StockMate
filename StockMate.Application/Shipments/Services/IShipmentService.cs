using StockMate.Application.Shipments.Models;
using StockMate.Domain.Enums;

namespace StockMate.Application.Shipments.Services;

public interface IShipmentService
{
    Task<ShipmentDto> CreateReadyShipmentAsync(
        long orderNumber,
        Guid workerId,
        Guid adminId,
        CancellationToken cancellationToken = default);

    Task<ShipmentDto> ChangeStatusAsync(
        Guid shipmentId,
        Guid actorStaffId,
        ShipmentStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentDto>> GetWorkerShipmentsAsync(
        Guid workerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShipmentDto>> GetOrderShipmentsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);
}

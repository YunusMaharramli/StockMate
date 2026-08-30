using StockMate.Domain.Enums;

namespace StockMate.Application.Notifications;

public interface IWorkflowNotificationService
{
    Task NotifyOrderConfirmedAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task NotifyOrderAssignedAsync(
        long orderNumber,
        long workerTelegramUserId,
        string workerName,
        CancellationToken cancellationToken = default);

    Task NotifyShipmentStatusChangedAsync(
        long orderNumber,
        ShipmentStatus status,
        string workerName,
        CancellationToken cancellationToken = default);
}

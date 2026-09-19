namespace StockMate.Application.Common.Outbox;

public static class OutboxMessageTypes
{
    public const string OrderCreated =
        "order.created";

    public const string OrderConfirmed =
        "order.confirmed";

    public const string OrderAssigned =
        "order.assigned";

    public const string ShipmentStatusChanged =
        "shipment.status-changed";

    public const string TelegramMessage =
        "telegram.message";
}

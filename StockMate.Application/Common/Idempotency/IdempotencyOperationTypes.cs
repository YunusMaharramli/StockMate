namespace StockMate.Application.Common.Idempotency;

public static class IdempotencyOperationTypes
{
    public const string CustomerCheckout =
        "customer.checkout";

    public const string OrderConfirm =
        "order.confirm";

    public const string OrderCancel =
        "order.cancel";

    public const string OrderCancelRemaining =
        "order.cancel-remaining";

    public const string OrderDeliverReady =
        "order.deliver-ready";

    public const string OrderDeliveryModeChange =
        "order.delivery-mode-change";

    public const string OrderStatusChange =
        "order.status-change";

    public const string SpecialOrderStatusChange =
        "order.special-status-change";

    public const string OrderComplete =
        "order.complete";

    public const string ProductStockChange =
        "product.stock-change";

    public const string StockPurchaseCreate =
        "stock-purchase.create";

    public const string OrderAssignmentCreate =
        "order-assignment.create";

    public const string OrderAssignmentRelease =
        "order-assignment.release";

    public const string ShipmentCreate =
        "shipment.create";

    public const string ShipmentStatusChange =
        "shipment.status-change";
}

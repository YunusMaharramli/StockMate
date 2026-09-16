namespace StockMate.Domain.Entities;

public class OrderShipmentItem
{
    private OrderShipmentItem() { }

    public OrderShipmentItem(Guid shipmentId, Guid orderItemId, int quantity)
    {
        if (shipmentId == Guid.Empty) throw new ArgumentException("ID доставки обязателен.", nameof(shipmentId));
        if (orderItemId == Guid.Empty) throw new ArgumentException("ID позиции заказа обязателен.", nameof(orderItemId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.NewGuid();
        ShipmentId = shipmentId;
        OrderItemId = orderItemId;
        Quantity = quantity;
    }

    public Guid Id { get; private set; }
    public Guid ShipmentId { get; private set; }
    public Guid OrderItemId { get; private set; }
    public int Quantity { get; private set; }
}

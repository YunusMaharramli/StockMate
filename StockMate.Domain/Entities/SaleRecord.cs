namespace StockMate.Domain.Entities;

public sealed class SaleRecord
{
    private readonly List<SaleRecordItem> _items = [];

    private SaleRecord()
    {
    }

    public SaleRecord(
        Order order,
        DateTime soldAtUtc,
        Guid? shipmentId = null)
    {
        if (soldAtUtc == default)
        {
            throw new ArgumentException(
                "Дата продажи обязательна.",
                nameof(soldAtUtc));
        }

        Id = Guid.NewGuid();
        OrderId = order.Id;
        OrderNumber = order.OrderNumber;
        ShipmentId = shipmentId;
        DeliveryCompanyId = order.DeliveryCompanyId;
        DeliveryCompanyName = order.DeliveryCompanyName;

        SoldAtUtc = soldAtUtc.Kind == DateTimeKind.Utc
            ? soldAtUtc
            : soldAtUtc.ToUniversalTime();

        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public long OrderNumber { get; private set; }

    public Guid? ShipmentId { get; private set; }

    public Guid? DeliveryCompanyId { get; private set; }

    public string? DeliveryCompanyName { get; private set; }

    public DateTime SoldAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<SaleRecordItem> Items =>
        _items.AsReadOnly();

    public decimal TotalAmount =>
        _items.Sum(item => item.LineTotal);

    public void AddItem(
        OrderItem orderItem,
        int quantity)
    {
        if (orderItem.OrderId != OrderId)
        {
            throw new InvalidOperationException(
                "Позиция заказа не относится к этой продаже.");
        }

        if (_items.Any(item =>
                item.OrderItemId == orderItem.Id))
        {
            throw new InvalidOperationException(
                "Позиция заказа уже есть в этой записи о продаже.");
        }

        var item = new SaleRecordItem(
            Id,
            orderItem,
            quantity);

        _items.Add(item);
    }
}
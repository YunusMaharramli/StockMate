using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class Order
{
    private readonly List<OrderItem> _items = [];

    private Order()
    {
    }

    public Order(
        Guid customerId,
        string phoneNumber,
        string deliveryAddress,
        double? latitude = null,
        double? longitude = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("ID клиента обязателен.", nameof(customerId));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Номер телефона обязателен.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new ArgumentException("Адрес доставки обязателен.", nameof(deliveryAddress));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        PhoneNumber = phoneNumber.Trim();
        DeliveryAddress = deliveryAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Status = OrderStatus.PendingAdminApproval;
        DeliveryMode = DeliveryMode.Undecided;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public uint Version { get; private set; }
    public long OrderNumber { get; private set; }

    public Guid CustomerId { get; private set; }
    public Guid? CustomerAddressId { get; private set; }
    public Guid? DeliveryCompanyId { get; private set; }
    public string? DeliveryCompanyName { get; private set; }

    public string PhoneNumber { get; private set; } = string.Empty;

    public string DeliveryAddress { get; private set; } = string.Empty;

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public OrderStatus Status { get; private set; }

    public DeliveryMode DeliveryMode { get; private set; }

    public decimal? DeliveryFee { get; private set; }

    public string? AdminNote { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ConfirmedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal ProductsTotal => _items.Sum(item => item.LineTotal);

    public decimal TotalAmount => ProductsTotal + (DeliveryFee ?? 0);

    public bool ContainsSpecialOrder =>
        _items.Any(item => item.SpecialOrderQuantity > 0);

    public FulfillmentStatus FulfillmentStatus =>
        _items.All(item => item.DeliveredQuantity == item.TotalQuantity)
            ? FulfillmentStatus.FullyDelivered
            : _items.All(item => item.RemainingQuantity == 0)
                ? FulfillmentStatus.ClosedWithShortage
                : _items.Any(item => item.DeliveredQuantity > 0)
                    ? FulfillmentStatus.PartiallyDelivered
                    : FulfillmentStatus.Waiting;

    public int RemainingQuantity => _items.Sum(item => item.RemainingQuantity);

    public int ReadyQuantity => _items.Sum(item => item.ReadyQuantity);

    public void AddItem(
        Product product,
        int fromStockQuantity,
        int specialOrderQuantity)
    {
        if (Status != OrderStatus.PendingAdminApproval)
            throw new InvalidOperationException(
                "Товары можно добавлять только до подтверждения администратором.");

        if (!product.IsActive)
            throw new InvalidOperationException("Неактивный товар нельзя заказать.");

        if (fromStockQuantity > product.StockQuantity)
            throw new InvalidOperationException(
                "Запрошенное количество превышает доступный остаток.");

        if (_items.Any(item => item.ProductId == product.Id))
            throw new InvalidOperationException(
                "Товар уже есть в этом заказе.");

        var item = new OrderItem(
            Id,
            product.Id,
            product.Name,
            product.Code,
            product.Price,
            fromStockQuantity,
            specialOrderQuantity);

        _items.Add(item);
    }

    public void SetDeliveryFee(decimal deliveryFee)
    {
        if (deliveryFee < 0)
            throw new ArgumentOutOfRangeException(nameof(deliveryFee));

        DeliveryFee = deliveryFee;
    }

    public void SetAdminNote(string? adminNote)
    {
        AdminNote = string.IsNullOrWhiteSpace(adminNote)
            ? null
            : adminNote.Trim();
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.PendingAdminApproval);

        if (_items.Count == 0)
            throw new InvalidOperationException("Нельзя подтвердить пустой заказ.");

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsPreparing()
    {
        EnsureStatus(OrderStatus.Confirmed);
        Status = OrderStatus.Preparing;
    }

    public void MarkAsDelivering()
    {
        EnsureStatus(OrderStatus.Preparing);
        Status = OrderStatus.Delivering;
    }

    public void Complete()
    {
        if (_items.Any(item => item.RemainingQuantity > 0))
            throw new InvalidOperationException(
                "Order still contains undelivered products.");

        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void SetDeliveryMode(DeliveryMode deliveryMode)
    {
        if (deliveryMode == DeliveryMode.Undecided)
            throw new ArgumentOutOfRangeException(nameof(deliveryMode));

        if (_items.Any(item => item.DeliveredQuantity > 0))
            throw new InvalidOperationException(
                "Режим доставки нельзя изменить после первой доставки.");

        DeliveryMode = deliveryMode;
    }

    public IReadOnlyDictionary<Guid, int> DeliverReadyItems()
    {
        if (Status is OrderStatus.PendingAdminApproval or
            OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Заказ нужно подтвердить до доставки.");
        }

        if (DeliveryMode != DeliveryMode.PartialAllowed &&
            ReadyQuantity < RemainingQuantity)
        {
            throw new InvalidOperationException(
                "Для этого заказа выбрана доставка всех товаров вместе.");
        }

        var delivered = new Dictionary<Guid, int>();

        foreach (var item in _items)
        {
            var quantity = item.DeliverReadyQuantity();

            if (quantity > 0)
                delivered[item.Id] = quantity;
        }

        if (delivered.Count == 0)
            throw new InvalidOperationException("Нет товаров, готовых к доставке.");

        if (_items.All(item => item.RemainingQuantity == 0))
        {
            Status = OrderStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            Status = OrderStatus.Preparing;
        }

        return delivered;
    }

    public void DeliverShipmentItems(
        IReadOnlyDictionary<Guid, int> quantities)
    {
        if (Status is OrderStatus.PendingAdminApproval or
            OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Перед доставкой заказ должен быть активен.");
        }

        if (quantities.Count == 0)
            throw new InvalidOperationException("Доставка пуста.");

        foreach (var entry in quantities)
        {
            var item = _items.FirstOrDefault(item => item.Id == entry.Key)
                ?? throw new KeyNotFoundException(
                    $"Элемент заказа '{entry.Key}' не найден.");

            item.DeliverQuantity(entry.Value);
        }

        if (_items.All(item => item.RemainingQuantity == 0))
        {
            Status = OrderStatus.Completed;
            CompletedAtUtc = DateTime.UtcNow;
        }
        else
        {
            Status = OrderStatus.Preparing;
        }
    }

    public int CancelRemainingItems()
    {
        if (Status is OrderStatus.PendingAdminApproval or
            OrderStatus.Completed or OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "В текущем статусе нельзя отменить оставшиеся товары.");
        }

        var cancelled = _items.Sum(item => item.CancelRemainingQuantity());

        if (cancelled == 0)
            throw new InvalidOperationException("В заказе не осталось товаров.");

        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;

        return cancelled;
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException(
                "Завершённый или отменённый заказ нельзя отменить.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Причина отмены обязательна.",
                nameof(reason));

        if (_items.Any(item => item.DeliveredQuantity > 0))
            throw new InvalidOperationException(
                "Частично доставленный заказ нельзя отменить полностью. " +
                "Отмените только оставшиеся товары.");

        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAtUtc = DateTime.UtcNow;
    }

    private void EnsureStatus(OrderStatus requiredStatus)
    {
        if (Status != requiredStatus)
        {
            throw new InvalidOperationException(
                $"Заказ должен иметь статус {requiredStatus}.");
        }
    }
    public void SelectDeliveryDetails(
    CustomerAddress address,
    DeliveryCompany company)
    {
        if (Status != OrderStatus.PendingAdminApproval)
        {
            throw new InvalidOperationException(
                "Данные доставки можно выбрать только до подтверждения администратором.");
        }

        if (address.CustomerId != CustomerId)
        {
            throw new InvalidOperationException(
                "Выбранный адрес не принадлежит этому клиенту.");
        }

        if (!address.IsActive)
        {
            throw new InvalidOperationException(
                "Неактивный адрес нельзя выбрать.");
        }

        if (!company.IsActive)
        {
            throw new InvalidOperationException(
                "Неактивную службу доставки нельзя выбрать.");
        }

        CustomerAddressId = address.Id;
        DeliveryAddress = address.AddressLine;
        Latitude = address.Latitude;
        Longitude = address.Longitude;

        DeliveryCompanyId = company.Id;
        DeliveryCompanyName = company.Name;
    }
}

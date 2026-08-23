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
            throw new ArgumentException("Customer ID is required.", nameof(customerId));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new ArgumentException("Delivery address is required.", nameof(deliveryAddress));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        PhoneNumber = phoneNumber.Trim();
        DeliveryAddress = deliveryAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Status = OrderStatus.PendingAdminApproval;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public long OrderNumber { get; private set; }

    public Guid CustomerId { get; private set; }

    public string PhoneNumber { get; private set; } = string.Empty;

    public string DeliveryAddress { get; private set; } = string.Empty;

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public OrderStatus Status { get; private set; }

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

    public void AddItem(
        Product product,
        int fromStockQuantity,
        int specialOrderQuantity)
    {
        if (Status != OrderStatus.PendingAdminApproval)
            throw new InvalidOperationException(
                "Items can only be added before admin approval.");

        if (!product.IsActive)
            throw new InvalidOperationException("Inactive product cannot be ordered.");

        if (fromStockQuantity > product.StockQuantity)
            throw new InvalidOperationException(
                "Requested stock quantity exceeds the available stock.");

        if (_items.Any(item => item.ProductId == product.Id))
            throw new InvalidOperationException(
                "Product already exists in this order.");

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
            throw new InvalidOperationException("Empty order cannot be confirmed.");

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
        EnsureStatus(OrderStatus.Delivering);

        Status = OrderStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException(
                "Completed or cancelled order cannot be cancelled.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "Cancellation reason is required.",
                nameof(reason));

        Status = OrderStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAtUtc = DateTime.UtcNow;
    }

    private void EnsureStatus(OrderStatus requiredStatus)
    {
        if (Status != requiredStatus)
        {
            throw new InvalidOperationException(
                $"Order must be in {requiredStatus} status.");
        }
    }
}

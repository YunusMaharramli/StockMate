using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class Product
{
    private Product()
    {
    }

    public Product(
        string name,
        string code,
        decimal price,
        int stockQuantity,
        string description,
        string? imageReference = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Product code is required.", nameof(code));

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        if (stockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(stockQuantity));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Price = price;
        StockQuantity = stockQuantity;
        Description = description.Trim();
        ImageReference = imageReference;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int StockQuantity { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? ImageReference { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void UpdateDetails(
    string name,
    string description,
    string? imageReference)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Name = name.Trim();
        Description = description.Trim();
        ImageReference = imageReference;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice));

        Price = newPrice;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public StockMovement AddStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var previousQuantity = StockQuantity;
        StockQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return new StockMovement(
            Id,
            StockMovementType.StockAdded,
            previousQuantity,
            StockQuantity,
            reason);
    }

    public StockMovement RemoveStock(int quantity, string reason)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > StockQuantity)
            throw new InvalidOperationException(
                "Quantity cannot exceed the available stock.");

        var previousQuantity = StockQuantity;
        StockQuantity -= quantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return new StockMovement(
            Id,
            StockMovementType.StockRemoved,
            previousQuantity,
            StockQuantity,
            reason);
    }

    public StockMovement SetStock(int quantity, string reason)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var previousQuantity = StockQuantity;
        StockQuantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return new StockMovement(
            Id,
            StockMovementType.StockCorrected,
            previousQuantity,
            StockQuantity,
            reason);
    }

    public StockMovement DeductForOrder(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        if (quantity > StockQuantity)
            throw new InvalidOperationException(
                "There is not enough stock to confirm the order.");

        var previousQuantity = StockQuantity;
        StockQuantity -= quantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return new StockMovement(
            Id,
            StockMovementType.OrderConfirmed,
            previousQuantity,
            StockQuantity,
            $"Stock deducted for order {orderId}.",
            orderId);
    }

    public StockMovement RestoreFromCancelledOrder(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        var previousQuantity = StockQuantity;
        StockQuantity += quantity;
        UpdatedAtUtc = DateTime.UtcNow;

        return new StockMovement(
            Id,
            StockMovementType.OrderCancelled,
            previousQuantity,
            StockQuantity,
            $"Stock restored from cancelled order {orderId}.",
            orderId);
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
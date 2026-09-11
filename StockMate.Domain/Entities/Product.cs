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
        string? imageReference = null,
        Guid? categoryId = null,
        Guid? subcategoryId = null,
        int minimumStockQuantity = 0)
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

        if (minimumStockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumStockQuantity));

        if (subcategoryId.HasValue && !categoryId.HasValue)
            throw new ArgumentException(
                "A subcategory cannot be assigned without a category.",
                nameof(subcategoryId));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Price = price;
        StockQuantity = stockQuantity;
        Description = description.Trim();
        ImageReference = imageReference;
        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        MinimumStockQuantity = minimumStockQuantity;
        IsActive = true;
        IsArchived = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public int StockQuantity { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string? ImageReference { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public Guid? SubcategoryId { get; private set; }

    public Subcategory? Subcategory { get; private set; }

    public int MinimumStockQuantity { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsArchived { get; private set; }

    public bool IsCatalogVisible =>
        IsActive &&
        !IsArchived &&
        (Category is null || (Category.IsActive && !Category.IsArchived)) &&
        (Subcategory is null || (Subcategory.IsActive && !Subcategory.IsArchived));

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

    public void ChangeClassification(
        Guid categoryId,
        Guid? subcategoryId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("Category ID is required.", nameof(categoryId));

        CategoryId = categoryId;
        SubcategoryId = subcategoryId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetMinimumStockQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        MinimumStockQuantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsArchived = false;
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

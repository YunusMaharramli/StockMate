using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class OrderItem
{
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid productId,
        string productName,
        string productCode,
        decimal unitPrice,
        int fromStockQuantity,
        int specialOrderQuantity)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Product code is required.", nameof(productCode));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (fromStockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(fromStockQuantity));

        if (specialOrderQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(specialOrderQuantity));

        if (fromStockQuantity + specialOrderQuantity == 0)
            throw new ArgumentException("Order item quantity must be greater than zero.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName.Trim();
        ProductCode = productCode.Trim().ToUpperInvariant();
        UnitPrice = unitPrice;
        FromStockQuantity = fromStockQuantity;
        SpecialOrderQuantity = specialOrderQuantity;

        SpecialOrderStatus = specialOrderQuantity > 0
            ? SpecialOrderStatus.Pending
            : SpecialOrderStatus.NotRequired;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string ProductCode { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int FromStockQuantity { get; private set; }

    public int SpecialOrderQuantity { get; private set; }

    public int TotalQuantity =>
        FromStockQuantity + SpecialOrderQuantity;

    public decimal LineTotal =>
        UnitPrice * TotalQuantity;

    public SpecialOrderStatus SpecialOrderStatus { get; private set; }

    public void MarkAsOrderedFromSupplier()
    {
        EnsureSpecialOrderExists();

        SpecialOrderStatus = SpecialOrderStatus.OrderedFromSupplier;
    }

    public void MarkSpecialOrderAsArrived()
    {
        EnsureSpecialOrderExists();

        SpecialOrderStatus = SpecialOrderStatus.Arrived;
    }

    public void CompleteSpecialOrder()
    {
        EnsureSpecialOrderExists();

        SpecialOrderStatus = SpecialOrderStatus.Completed;
    }

    public void CancelSpecialOrder()
    {
        EnsureSpecialOrderExists();

        SpecialOrderStatus = SpecialOrderStatus.Cancelled;
    }

    private void EnsureSpecialOrderExists()
    {
        if (SpecialOrderQuantity == 0)
            throw new InvalidOperationException(
                "This item does not contain a special-order quantity.");
    }
}
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
            throw new ArgumentException("ID заказа обязателен.", nameof(orderId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ID товара обязателен.", nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Название товара обязательно.", nameof(productName));

        if (string.IsNullOrWhiteSpace(productCode))
            throw new ArgumentException("Код товара обязателен.", nameof(productCode));

        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice));

        if (fromStockQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(fromStockQuantity));

        if (specialOrderQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(specialOrderQuantity));

        if (fromStockQuantity + specialOrderQuantity == 0)
            throw new ArgumentException("Количество позиции заказа должно быть больше нуля.");

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

    public int DeliveredQuantity { get; private set; }

    public int CancelledQuantity { get; private set; }

    public int TotalQuantity =>
        FromStockQuantity + SpecialOrderQuantity;

    public decimal LineTotal =>
        UnitPrice * TotalQuantity;

    public int RemainingQuantity =>
        TotalQuantity - DeliveredQuantity - CancelledQuantity;

    public int ReadyQuantity => Math.Max(
        0,
        FromStockQuantity +
        (SpecialOrderStatus is SpecialOrderStatus.Arrived or SpecialOrderStatus.Completed
            ? SpecialOrderQuantity
            : 0) -
        DeliveredQuantity -
        CancelledQuantity);

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

    internal int DeliverReadyQuantity()
    {
        var quantity = Math.Min(ReadyQuantity, RemainingQuantity);

        if (quantity > 0)
            DeliveredQuantity += quantity;

        if (RemainingQuantity == 0 &&
            SpecialOrderStatus == SpecialOrderStatus.Arrived)
        {
            SpecialOrderStatus = SpecialOrderStatus.Completed;
        }

        return quantity;
    }

    internal void DeliverQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > ReadyQuantity || quantity > RemainingQuantity)
            throw new InvalidOperationException(
                "Shipment quantity exceeds the ready quantity.");

        DeliveredQuantity += quantity;

        if (RemainingQuantity == 0 &&
            SpecialOrderStatus == SpecialOrderStatus.Arrived)
        {
            SpecialOrderStatus = SpecialOrderStatus.Completed;
        }
    }

    internal int CancelRemainingQuantity()
    {
        var quantity = RemainingQuantity;

        if (quantity > 0)
            CancelledQuantity += quantity;

        if (quantity > 0 &&
            SpecialOrderQuantity > 0 &&
            SpecialOrderStatus is not SpecialOrderStatus.Completed)
        {
            SpecialOrderStatus = SpecialOrderStatus.Cancelled;
        }

        return quantity;
    }

    private void EnsureSpecialOrderExists()
    {
        if (SpecialOrderQuantity == 0)
            throw new InvalidOperationException(
                "Эта позиция не содержит товаров под заказ.");
    }
}

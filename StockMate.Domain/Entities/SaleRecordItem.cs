namespace StockMate.Domain.Entities;

public sealed class SaleRecordItem
{
    private SaleRecordItem()
    {
    }

    internal SaleRecordItem(
        Guid saleRecordId,
        OrderItem orderItem,
        int quantity)
    {
        if (saleRecordId == Guid.Empty)
        {
            throw new ArgumentException(
                "Sale record ID is required.",
                nameof(saleRecordId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (quantity > orderItem.TotalQuantity)
        {
            throw new InvalidOperationException(
                "Sale quantity cannot exceed the ordered quantity.");
        }

        Id = Guid.NewGuid();
        SaleRecordId = saleRecordId;
        OrderItemId = orderItem.Id;
        ProductId = orderItem.ProductId;
        ProductName = orderItem.ProductName;
        ProductCode = orderItem.ProductCode;
        Quantity = quantity;
        UnitPrice = orderItem.UnitPrice;
    }

    public Guid Id { get; private set; }

    public Guid SaleRecordId { get; private set; }

    public Guid OrderItemId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string ProductCode { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
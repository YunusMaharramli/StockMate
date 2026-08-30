namespace StockMate.Domain.Entities;

public sealed class StockPurchaseItem
{
    private StockPurchaseItem()
    {
    }

    internal StockPurchaseItem(
        Guid stockPurchaseId,
        Product product,
        int quantity,
        decimal unitCost)
    {
        if (stockPurchaseId == Guid.Empty)
        {
            throw new ArgumentException(
                "Stock purchase ID is required.",
                nameof(stockPurchaseId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Quantity must be greater than zero.");
        }

        if (unitCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCost),
                "Unit cost cannot be negative.");
        }

        Id = Guid.NewGuid();
        StockPurchaseId = stockPurchaseId;
        ProductId = product.Id;
        ProductName = product.Name;
        ProductCode = product.Code;
        Quantity = quantity;
        UnitCost = unitCost;
    }

    public Guid Id { get; private set; }

    public Guid StockPurchaseId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string ProductCode { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitCost { get; private set; }

    public decimal LineTotal => Quantity * UnitCost;
}
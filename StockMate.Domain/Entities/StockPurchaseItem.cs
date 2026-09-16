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
                "ID закупки обязателен.",
                nameof(stockPurchaseId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                "Количество должно быть больше нуля.");
        }

        if (unitCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitCost),
                "Цена за единицу не может быть отрицательной.");
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
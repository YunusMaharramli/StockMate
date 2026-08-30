namespace StockMate.Domain.Entities;

public sealed class StockPurchase
{
    private readonly List<StockPurchaseItem> _items = [];

    private StockPurchase()
    {
    }

    public StockPurchase(
        DateTime purchasedAtUtc,
        string? note = null)
    {
        if (purchasedAtUtc == default)
        {
            throw new ArgumentException(
                "Purchase date is required.",
                nameof(purchasedAtUtc));
        }

        Id = Guid.NewGuid();

        PurchasedAtUtc = purchasedAtUtc.Kind == DateTimeKind.Utc
            ? purchasedAtUtc
            : purchasedAtUtc.ToUniversalTime();

        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();

        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public DateTime PurchasedAtUtc { get; private set; }

    public string? Note { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<StockPurchaseItem> Items =>
        _items.AsReadOnly();

    public decimal TotalAmount =>
        _items.Sum(item => item.LineTotal);

    public void AddItem(
        Product product,
        int quantity,
        decimal unitCost)
    {
        if (_items.Any(item =>
                item.ProductId == product.Id))
        {
            throw new InvalidOperationException(
                "The product already exists in this purchase.");
        }

        var item = new StockPurchaseItem(
            Id,
            product,
            quantity,
            unitCost);

        _items.Add(item);
    }

    public void UpdateNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
    }
}
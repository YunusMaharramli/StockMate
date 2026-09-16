using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class Cart
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly List<CartItem> _items = [];

    private Cart()
    {
    }

    public Cart(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("ID клиента обязателен.", nameof(customerId));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
        ExpiresAtUtc = CreatedAtUtc.Add(Lifetime);
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public CartItem AddOrUpdateItem(
    Guid productId,
    int quantity,
    CartItemFulfillmentMode fulfillmentMode)
    {
        if (IsExpired)
            throw new InvalidOperationException("Срок действия корзины истёк.");

        if (productId == Guid.Empty)
            throw new ArgumentException("ID товара обязателен.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        var existingItem = _items.FirstOrDefault(
    item => item.ProductId == productId);

        if (existingItem is null)
        {
            existingItem = new CartItem(
                Id,
                productId,
                quantity,
                fulfillmentMode);

            _items.Add(existingItem);
        }
        else
        {
            existingItem.ChangeQuantity(quantity);
            existingItem.ChangeFulfillmentMode(fulfillmentMode);
        }

        Touch();

        return existingItem;
    }

    public void RemoveItem(Guid productId)
    {
        var item = _items.FirstOrDefault(
            existingItem => existingItem.ProductId == productId);

        if (item is null)
            return;

        _items.Remove(item);
        Touch();
    }

    public void Clear()
    {
        _items.Clear();
        Touch();
    }

    public void Renew()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = UpdatedAtUtc.Add(Lifetime);
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = UpdatedAtUtc.Add(Lifetime);
    }
}
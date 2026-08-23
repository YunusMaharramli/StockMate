using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class CartItem
{
    private CartItem()
    {
    }

    internal CartItem(
        Guid cartId,
        Guid productId,
        int quantity,
        CartItemFulfillmentMode fulfillmentMode)
    {
        if (cartId == Guid.Empty)
            throw new ArgumentException("Cart ID is required.", nameof(cartId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.NewGuid();
        CartId = cartId;
        ProductId = productId;
        Quantity = quantity;
        FulfillmentMode = fulfillmentMode;
    }

    public Guid Id { get; private set; }

    public Guid CartId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    public CartItemFulfillmentMode FulfillmentMode { get; private set; }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Quantity = quantity;
    }

    internal void ChangeFulfillmentMode(
        CartItemFulfillmentMode fulfillmentMode)
    {
        FulfillmentMode = fulfillmentMode;
    }
}
using StockMate.Domain.Enums;

namespace StockMate.Application.Carts.Models;

public sealed record AddCartItemRequest(
    Guid ProductId,
    int Quantity,
    CartItemFulfillmentMode FulfillmentMode);

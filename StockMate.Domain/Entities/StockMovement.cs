using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class StockMovement
{
    private StockMovement()
    {
    }

    public StockMovement(
        Guid productId,
        StockMovementType type,
        int previousQuantity,
        int newQuantity,
        string reason,
        Guid? orderId = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (previousQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(previousQuantity));

        if (newQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(newQuantity));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Movement reason is required.", nameof(reason));

        Id = Guid.NewGuid();
        ProductId = productId;
        Type = type;
        PreviousQuantity = previousQuantity;
        NewQuantity = newQuantity;
        QuantityChange = newQuantity - previousQuantity;
        Reason = reason.Trim();
        OrderId = orderId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public StockMovementType Type { get; private set; }

    public int PreviousQuantity { get; private set; }

    public int NewQuantity { get; private set; }

    public int QuantityChange { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public Guid? OrderId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
}
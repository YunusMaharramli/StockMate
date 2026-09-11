using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Domain.Tests.Products;

public class ProductTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveProduct()
    {
        var product = CreateProduct(stockQuantity: 10);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("CC-1L", product.Code);
        Assert.Equal(10, product.StockQuantity);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenStockIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateProduct(stockQuantity: -1));
    }

    [Fact]
    public void AddStock_ShouldIncreaseQuantity()
    {
        var product = CreateProduct(stockQuantity: 10);

        var movement = product.AddStock(
            quantity: 5,
            reason: "New stock received.");

        Assert.Equal(15, product.StockQuantity);
        Assert.Equal(10, movement.PreviousQuantity);
        Assert.Equal(15, movement.NewQuantity);
        Assert.Equal(5, movement.QuantityChange);
        Assert.Equal(
            StockMovementType.StockAdded,
            movement.Type);
    }

    [Fact]
    public void RemoveStock_ShouldDecreaseQuantity()
    {
        var product = CreateProduct(stockQuantity: 10);

        var movement = product.RemoveStock(
            quantity: 3,
            reason: "Damaged products.");

        Assert.Equal(7, product.StockQuantity);
        Assert.Equal(-3, movement.QuantityChange);
        Assert.Equal(
            StockMovementType.StockRemoved,
            movement.Type);
    }

    [Fact]
    public void RemoveStock_ShouldThrow_WhenQuantityExceedsStock()
    {
        var product = CreateProduct(stockQuantity: 3);

        Assert.Throws<InvalidOperationException>(() =>
            product.RemoveStock(
                quantity: 4,
                reason: "Invalid removal."));
    }

    [Fact]
    public void DeductForOrder_ShouldDecreaseStock()
    {
        var product = CreateProduct(stockQuantity: 10);
        var orderId = Guid.NewGuid();

        var movement = product.DeductForOrder(
            quantity: 4,
            orderId);

        Assert.Equal(6, product.StockQuantity);
        Assert.Equal(orderId, movement.OrderId);
        Assert.Equal(
            StockMovementType.OrderConfirmed,
            movement.Type);
    }

    [Fact]
    public void RestoreFromCancelledOrder_ShouldRestoreStock()
    {
        var product = CreateProduct(stockQuantity: 6);
        var orderId = Guid.NewGuid();

        var movement = product.RestoreFromCancelledOrder(
            quantity: 4,
            orderId);

        Assert.Equal(10, product.StockQuantity);
        Assert.Equal(4, movement.QuantityChange);
        Assert.Equal(
            StockMovementType.OrderCancelled,
            movement.Type);
    }

    [Fact]
    public void ClassificationAndMinimumStock_ShouldBeStored()
    {
        var product = CreateProduct(stockQuantity: 3);
        var categoryId = Guid.NewGuid();
        var subcategoryId = Guid.NewGuid();

        product.ChangeClassification(categoryId, subcategoryId);
        product.SetMinimumStockQuantity(5);

        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(subcategoryId, product.SubcategoryId);
        Assert.Equal(5, product.MinimumStockQuantity);
    }

    [Fact]
    public void ArchiveAndRestore_ShouldPreserveIndividualActiveState()
    {
        var product = CreateProduct(stockQuantity: 3);
        product.Deactivate();

        product.Archive();
        product.Restore();

        Assert.False(product.IsArchived);
        Assert.False(product.IsActive);
    }

    private static Product CreateProduct(int stockQuantity)
    {
        return new Product(
            name: "Coca-Cola 1 L",
            code: "cc-1l",
            price: 1.80m,
            stockQuantity,
            description: "Qazlı sərinləşdirici içki.");
    }
}

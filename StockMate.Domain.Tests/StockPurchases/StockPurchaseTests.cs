using StockMate.Domain.Entities;

namespace StockMate.Domain.Tests.StockPurchases;

public class StockPurchaseTests
{
    [Fact]
    public void AddItem_ShouldCalculatePurchaseTotal()
    {
        var purchase = new StockPurchase(
            DateTime.UtcNow,
            "Test alış");

        var cocaCola = CreateProduct(
            "Coca-Cola 1 L",
            "CC-1L");

        var fanta = CreateProduct(
            "Fanta 1 L",
            "FN-1L");

        purchase.AddItem(
            cocaCola,
            quantity: 20,
            unitCost: 1.20m);

        purchase.AddItem(
            fanta,
            quantity: 10,
            unitCost: 1.30m);

        Assert.Equal(2, purchase.Items.Count);
        Assert.Equal(37.00m, purchase.TotalAmount);
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenProductIsRepeated()
    {
        var purchase = new StockPurchase(
            DateTime.UtcNow);

        var product = CreateProduct(
            "Coca-Cola 1 L",
            "CC-1L");

        purchase.AddItem(
            product,
            quantity: 10,
            unitCost: 1.20m);

        Assert.Throws<InvalidOperationException>(() =>
            purchase.AddItem(
                product,
                quantity: 5,
                unitCost: 1.25m));
    }

    [Fact]
    public void AddItem_ShouldThrow_WhenQuantityIsZero()
    {
        var purchase = new StockPurchase(
            DateTime.UtcNow);

        var product = CreateProduct(
            "Coca-Cola 1 L",
            "CC-1L");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            purchase.AddItem(
                product,
                quantity: 0,
                unitCost: 1.20m));
    }

    private static Product CreateProduct(
        string name,
        string code)
    {
        return new Product(
            name,
            code,
            price: 1.80m,
            stockQuantity: 10,
            description: "Test məhsulu");
    }
}
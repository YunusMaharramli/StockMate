namespace StockMate.Bot.Telegram.State;

public enum AdminInputStep
{
    None = 0,
    AwaitingPrice = 1,
    AwaitingStockQuantity = 2,
    AwaitingStockReason = 3,

    AwaitingProductName = 10,
    AwaitingProductCode = 11,
    AwaitingProductPrice = 12,
    AwaitingInitialStock = 13,
    AwaitingProductDescription = 14,
    AwaitingProductImage = 15,
    AwaitingProductNameEdit = 20,
    AwaitingProductDescriptionEdit = 21,
    AwaitingProductImageEdit = 22
}
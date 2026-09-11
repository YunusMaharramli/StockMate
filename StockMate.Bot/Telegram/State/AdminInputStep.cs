namespace StockMate.Bot.Telegram.State;

public enum AdminInputStep
{
    None = 0,
    AwaitingPrice = 1,
    AwaitingStockQuantity = 2,
    AwaitingStockReason = 3,

    AwaitingProductNameEdit = 20,
    AwaitingProductDescriptionEdit = 21,
    AwaitingProductImageEdit = 22,

    AwaitingWorkerTelegramId = 30,
    AwaitingWorkerFirstName = 31,
    AwaitingWorkerLastName = 32,
    AwaitingWorkerUsername = 33
}

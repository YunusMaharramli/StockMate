namespace StockMate.Bot.Telegram.State;

public enum CheckoutStep
{
    None = 0,
    AwaitingPhone = 1,
    AwaitingAddress = 2,
    AwaitingLocation = 3
}
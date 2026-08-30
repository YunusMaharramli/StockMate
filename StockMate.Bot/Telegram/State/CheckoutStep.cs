namespace StockMate.Bot.Telegram.State;

public enum CheckoutStep
{
    None = 0,
    AwaitingPhone = 1,
    AwaitingAddress = 2,
    AwaitingLocation = 3,
    SelectingAddress = 4,
    AwaitingNewAddress = 5,
    AwaitingNewLocation = 6,
    SelectingDeliveryCompany = 7,
    AwaitingConfirmation = 8
}

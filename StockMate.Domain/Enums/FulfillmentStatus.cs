namespace StockMate.Domain.Enums;

public enum FulfillmentStatus
{
    Waiting = 1,
    PartiallyDelivered = 2,
    FullyDelivered = 3,
    ClosedWithShortage = 4
}

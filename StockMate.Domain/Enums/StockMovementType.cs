namespace StockMate.Domain.Enums;

public enum StockMovementType
{
    InitialStock = 1,
    StockAdded = 2,
    StockRemoved = 3,
    StockCorrected = 4,
    OrderConfirmed = 5,
    OrderCancelled = 6
}
namespace StockMate.Domain.Enums;

public enum SpecialOrderStatus
{
    NotRequired = 0,
    Pending = 1,
    OrderedFromSupplier = 2,
    Arrived = 3,
    Completed = 4,
    Cancelled = 5
}
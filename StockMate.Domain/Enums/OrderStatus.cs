namespace StockMate.Domain.Enums;

public enum OrderStatus
{
    PendingAdminApproval = 1,
    Confirmed = 2,
    Preparing = 3,
    Delivering = 4,
    Completed = 5,
    Cancelled = 6
}
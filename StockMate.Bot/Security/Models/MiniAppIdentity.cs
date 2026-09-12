namespace StockMate.Bot.Security.Models;

public sealed record MiniAppIdentity(
    long TelegramUserId,
    MiniAppUserRole Role,
    Guid? StaffId,
    Guid? CustomerId,
    string FirstName,
    string? LastName,
    string? Username);
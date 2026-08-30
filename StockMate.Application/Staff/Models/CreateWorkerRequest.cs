namespace StockMate.Application.Staff.Models;

public sealed record CreateWorkerRequest(
    long TelegramUserId,
    string FirstName,
    string? LastName,
    string? Username);

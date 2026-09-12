namespace StockMate.Bot.Security.Models;

public sealed record TelegramMiniAppUser(
    long TelegramUserId,
    string FirstName,
    string? LastName,
    string? Username);

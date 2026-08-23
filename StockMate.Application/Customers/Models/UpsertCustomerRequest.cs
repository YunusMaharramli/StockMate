namespace StockMate.Application.Customers.Models;

public sealed record UpsertCustomerRequest(
    long TelegramUserId,
    string FirstName,
    string? LastName,
    string? Username);
namespace StockMate.Application.Customers.Models;

public sealed record CustomerDto(
    Guid Id,
    long TelegramUserId,
    string FirstName,
    string? LastName,
    string? Username,
    string? PhoneNumber,
    string? DeliveryAddress,
    double? Latitude,
    double? Longitude,
    int ProductPageSize);
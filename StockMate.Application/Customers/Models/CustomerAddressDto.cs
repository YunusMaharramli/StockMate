namespace StockMate.Application.Customers.Models;

public sealed record CustomerAddressDto(
    Guid Id,
    string AddressLine,
    double? Latitude,
    double? Longitude,
    bool IsActive);
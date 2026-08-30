namespace StockMate.Application.Customers.Models;

public sealed record CreateCustomerAddressRequest(
    string AddressLine,
    double? Latitude,
    double? Longitude);
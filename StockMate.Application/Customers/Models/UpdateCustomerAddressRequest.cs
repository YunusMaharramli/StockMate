namespace StockMate.Application.Customers.Models;

public sealed record UpdateCustomerAddressRequest(
    string AddressLine,
    double? Latitude,
    double? Longitude);
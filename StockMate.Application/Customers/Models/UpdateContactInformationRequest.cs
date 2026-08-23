namespace StockMate.Application.Customers.Models;

public sealed record UpdateContactInformationRequest(
    string PhoneNumber,
    string DeliveryAddress,
    double? Latitude,
    double? Longitude);

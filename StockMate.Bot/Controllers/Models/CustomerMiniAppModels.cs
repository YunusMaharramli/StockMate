using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers.Models;

public sealed record CustomerCartItemResponse(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record CustomerCartResponse(
    Guid Id,
    IReadOnlyList<CustomerCartItemResponse> Items,
    decimal ProductsTotal,
    DateTime ExpiresAtUtc);

public sealed record SetCustomerCartItemRequest(
    Guid ProductId,
    int Quantity);

public sealed record CustomerProfileResponse(
    string FirstName,
    string? LastName,
    string? Username,
    string? PhoneNumber);

public sealed record UpdateCustomerPhoneRequest(string PhoneNumber);

public sealed record CustomerOrderItemResponse(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public sealed record CustomerOrderResponse(
    long OrderNumber,
    string PhoneNumber,
    string DeliveryAddress,
    string? DeliveryCompanyName,
    OrderStatus Status,
    IReadOnlyList<CustomerOrderItemResponse> Items,
    decimal ProductsTotal,
    decimal? DeliveryFee,
    decimal TotalAmount,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    DateTime? ConfirmedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc);

public sealed record CustomerCheckoutRequest(
    Guid CustomerAddressId,
    Guid DeliveryCompanyId);

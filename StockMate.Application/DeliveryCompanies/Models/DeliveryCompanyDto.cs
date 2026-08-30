namespace StockMate.Application.DeliveryCompanies.Models;

public sealed record DeliveryCompanyDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
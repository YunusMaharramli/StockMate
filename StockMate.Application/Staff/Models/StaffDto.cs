using StockMate.Domain.Enums;

namespace StockMate.Application.Staff.Models;

public sealed record StaffDto(
    Guid Id,
    long TelegramUserId,
    string FirstName,
    string? LastName,
    string? Username,
    StaffRole Role,
    bool IsActive,
    DateTime CreatedAtUtc);

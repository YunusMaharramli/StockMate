using StockMate.Application.Staff.Models;

namespace StockMate.Application.Staff.Services;

public interface IStaffService
{
    Task<StaffDto> EnsureAdminAsync(
        long telegramUserId,
        string firstName,
        string? lastName,
        string? username,
        CancellationToken cancellationToken = default);

    Task<StaffDto> CreateWorkerAsync(
        CreateWorkerRequest request,
        CancellationToken cancellationToken = default);

    Task<StaffDto?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task ActivateAsync(
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid staffId,
        CancellationToken cancellationToken = default);
}

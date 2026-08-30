using StaffEntity = StockMate.Domain.Entities.Staff;

namespace StockMate.Application.Persistence;

public interface IStaffRepository
{
    Task<StaffEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StaffEntity?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StaffEntity staff,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}

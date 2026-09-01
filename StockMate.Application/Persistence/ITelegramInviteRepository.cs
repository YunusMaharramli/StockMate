using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ITelegramInviteRepository
{
    Task<TelegramInvite?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TelegramInvite invite,
        CancellationToken cancellationToken = default);
    Task<bool> TryUseAsync(
    string token,
    long telegramUserId,
    CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
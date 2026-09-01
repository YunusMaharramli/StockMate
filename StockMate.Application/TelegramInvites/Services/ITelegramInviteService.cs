namespace StockMate.Application.TelegramInvites.Services;

public interface ITelegramInviteService
{
    Task<string> CreateAsync(
        CancellationToken cancellationToken = default);

    Task<bool> TryUseAsync(
        string token,
        long telegramUserId,
        CancellationToken cancellationToken = default);
}
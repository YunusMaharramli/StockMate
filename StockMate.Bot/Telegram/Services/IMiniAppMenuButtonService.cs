using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Telegram.Services;

public interface IMiniAppMenuButtonService
{
    Task SetAsync(
        long telegramUserId,
        MiniAppUserRole role,
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}
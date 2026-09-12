namespace StockMate.Bot.Telegram.Services;

public interface IMiniAppMenuButtonService
{
    Task ResetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}

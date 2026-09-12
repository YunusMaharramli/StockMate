namespace StockMate.Bot.Telegram.Services;

public interface IMiniAppMenuButtonService
{
    Task ResetDefaultAsync(
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}

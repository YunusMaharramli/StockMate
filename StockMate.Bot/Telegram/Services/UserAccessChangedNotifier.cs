using StockMate.Application.Notifications;

namespace StockMate.Bot.Telegram.Services;

public sealed class UserAccessChangedNotifier(
    IMiniAppMenuButtonService menuButtons)
    : IUserAccessChangedNotifier
{
    public Task NotifyAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        return menuButtons.ResetAsync(
            telegramUserId,
            cancellationToken);
    }
}

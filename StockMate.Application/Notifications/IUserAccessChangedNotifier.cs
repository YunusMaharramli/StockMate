namespace StockMate.Application.Notifications;

public interface IUserAccessChangedNotifier
{
    Task NotifyAsync(long telegramUserId, CancellationToken cancellationToken = default);
}

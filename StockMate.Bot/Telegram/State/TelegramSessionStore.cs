using System.Collections.Concurrent;

namespace StockMate.Bot.Telegram.State;

public class TelegramSessionStore
{
    private readonly ConcurrentDictionary<long, TelegramSession>
        _sessions = new();

    public TelegramSession GetOrCreate(long telegramUserId)
    {
        return _sessions.GetOrAdd(
            telegramUserId,
            _ => new TelegramSession());
    }

    public bool TryGet(
        long telegramUserId,
        out TelegramSession? session)
    {
        return _sessions.TryGetValue(
            telegramUserId,
            out session);
    }

    public void Clear(long telegramUserId)
    {
        _sessions.TryRemove(
            telegramUserId,
            out _);
    }
}
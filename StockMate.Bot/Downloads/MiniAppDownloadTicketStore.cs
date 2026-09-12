using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace StockMate.Bot.Downloads;

public sealed class MiniAppDownloadTicketStore
    : IMiniAppDownloadTicketStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset>
        _tickets = new(StringComparer.Ordinal);

    public string Create(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        RemoveExpiredTickets();

        var token = Convert
            .ToHexString(RandomNumberGenerator.GetBytes(32))
            .ToLowerInvariant();

        _tickets[token] = DateTimeOffset.UtcNow.Add(lifetime);

        return token;
    }

    public bool TryConsume(string token)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !_tickets.TryRemove(token, out var expiresAtUtc))
        {
            return false;
        }

        return expiresAtUtc > DateTimeOffset.UtcNow;
    }

    private void RemoveExpiredTickets()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var ticket in _tickets)
        {
            if (ticket.Value <= now)
                _tickets.TryRemove(ticket.Key, out _);
        }
    }
}

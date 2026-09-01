namespace StockMate.Domain.Entities;

public sealed class TelegramInvite
{
    private TelegramInvite()
    {
    }

    public TelegramInvite(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Invite token is required.",
                nameof(token));
        }

        Id = Guid.NewGuid();
        Token = token.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Token { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public long? UsedByTelegramUserId { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public bool IsUsed => UsedAtUtc.HasValue;

    public void Use(long telegramUserId)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(telegramUserId));
        }

        if (IsUsed)
        {
            throw new InvalidOperationException(
                "Invite link has already been used.");
        }

        UsedByTelegramUserId = telegramUserId;
        UsedAtUtc = DateTime.UtcNow;
    }
}
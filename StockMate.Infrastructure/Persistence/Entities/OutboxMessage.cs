namespace StockMate.Infrastructure.Persistence.Entities;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public OutboxMessage(
        string messageType,
        string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(messageType))
            throw new ArgumentException(
                "Message type is required.",
                nameof(messageType));

        if (string.IsNullOrWhiteSpace(payloadJson))
            throw new ArgumentException(
                "Message payload is required.",
                nameof(payloadJson));

        Id = Guid.NewGuid();
        MessageType = messageType.Trim();
        PayloadJson = payloadJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string MessageType { get; private set; } = string.Empty;

    public string PayloadJson { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ProcessedAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTime? NextAttemptAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public bool IsProcessed => ProcessedAtUtc.HasValue;

    public void MarkProcessed()
    {
        AttemptCount++;
        ProcessedAtUtc = DateTime.UtcNow;
        NextAttemptAtUtc = null;
        LastError = null;
    }

    public void MarkFailed(
        string error,
        DateTime nextAttemptAtUtc)
    {
        if (string.IsNullOrWhiteSpace(error))
            throw new ArgumentException(
                "Error message is required.",
                nameof(error));

        AttemptCount++;
        LastError = error.Trim();
        NextAttemptAtUtc = nextAttemptAtUtc;
    }
}
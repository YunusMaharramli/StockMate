namespace StockMate.Application.Common.Outbox;

public interface IOutboxMessageStore
{
    Task<IReadOnlyList<OutboxMessageData>> GetPendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTime nextAttemptAtUtc,
        CancellationToken cancellationToken = default);
}

public sealed record OutboxMessageData(
    Guid Id,
    string MessageType,
    string PayloadJson,
    int AttemptCount);
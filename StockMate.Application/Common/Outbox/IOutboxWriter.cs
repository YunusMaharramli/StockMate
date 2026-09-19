namespace StockMate.Application.Common.Outbox;

public interface IOutboxWriter
{
    Task EnqueueAsync<TPayload>(
        string messageType,
        TPayload payload,
        CancellationToken cancellationToken = default);
}
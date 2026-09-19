namespace StockMate.Application.Common.Idempotency;

public interface IIdempotencyService
{
    Task<TResult> ExecuteAsync<TResult>(
        Guid operationId,
        long telegramUserId,
        string operationType,
        object payload,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteAsync(
        Guid operationId,
        long telegramUserId,
        string operationType,
        object payload,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
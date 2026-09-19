using Microsoft.EntityFrameworkCore;
using StockMate.Application.Common.Outbox;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Outbox;

public sealed class OutboxMessageStore : IOutboxMessageStore
{
    private readonly StockMateDbContext _dbContext;

    public OutboxMessageStore(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OutboxMessageData>> GetPendingAsync(
        DateTime utcNow,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(batchSize));

        return await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message =>
                message.ProcessedAtUtc == null &&
                (message.NextAttemptAtUtc == null ||
                 message.NextAttemptAtUtc <= utcNow))
            .OrderBy(message => message.CreatedAtUtc)
            .Take(batchSize)
            .Select(message => new OutboxMessageData(
                message.Id,
                message.MessageType,
                message.PayloadJson,
                message.AttemptCount))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages
            .SingleOrDefaultAsync(
                value => value.Id == messageId,
                cancellationToken);

        if (message is null)
            return;

        message.MarkProcessed();

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        string error,
        DateTime nextAttemptAtUtc,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.OutboxMessages
            .SingleOrDefaultAsync(
                value => value.Id == messageId,
                cancellationToken);

        if (message is null)
            return;

        message.MarkFailed(
            error,
            nextAttemptAtUtc);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
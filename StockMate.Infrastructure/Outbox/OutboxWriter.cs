using System.Text.Json;
using StockMate.Application.Common.Outbox;
using StockMate.Infrastructure.Persistence;
using StockMate.Infrastructure.Persistence.Entities;

namespace StockMate.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly StockMateDbContext _dbContext;

    public OutboxWriter(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task EnqueueAsync<TPayload>(
        string messageType,
        TPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var payloadJson = JsonSerializer.Serialize(
            payload,
            JsonOptions);

        var message = new OutboxMessage(
            messageType,
            payloadJson);

        await _dbContext.OutboxMessages.AddAsync(
            message,
            cancellationToken);
    }
}
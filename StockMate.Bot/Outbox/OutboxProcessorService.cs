using StockMate.Application.Common.Outbox;

namespace StockMate.Bot.Outbox;

public sealed class OutboxProcessorService : BackgroundService
{
    private const int BatchSize = 20;

    private static readonly TimeSpan PollInterval =
        TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "The Outbox processing cycle failed.");
            }

            try
            {
                await Task.Delay(
                    PollInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task ProcessPendingMessagesAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var store = scope.ServiceProvider
            .GetRequiredService<IOutboxMessageStore>();

        var dispatcher = scope.ServiceProvider
            .GetRequiredService<OutboxMessageDispatcher>();

        var messages = await store.GetPendingAsync(
            DateTime.UtcNow,
            BatchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(
                    message,
                    cancellationToken);

                await store.MarkProcessedAsync(
                    message.Id,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var nextAttemptAtUtc =
                    CalculateNextAttemptAtUtc(
                        message.AttemptCount);

                var error = exception.ToString();

                if (error.Length > 2000)
                    error = error[..2000];

                await store.MarkFailedAsync(
                    message.Id,
                    error,
                    nextAttemptAtUtc,
                    cancellationToken);

                _logger.LogWarning(
                    exception,
                    "Outbox message {MessageId} failed. " +
                    "Next attempt: {NextAttemptAtUtc}.",
                    message.Id,
                    nextAttemptAtUtc);
            }
        }
    }

    private static DateTime CalculateNextAttemptAtUtc(
        int attemptCount)
    {
        var exponent = Math.Min(attemptCount, 6);
        var delaySeconds = Math.Min(
            Math.Pow(2, exponent) * 5,
            300);

        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }
}
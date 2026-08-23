using StockMate.Bot.Telegram.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StockMate.Bot.Telegram.Services;

public class TelegramPollingService : BackgroundService
{
    private readonly TelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramPollingService> _logger;

    public TelegramPollingService(
        TelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        ILogger<TelegramPollingService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var botInformation = await _botClient.GetMe(stoppingToken);

        _logger.LogInformation(
            "Telegram bot @{Username} started.",
            botInformation.Username);

        await _botClient.DropPendingUpdates(stoppingToken);

        _botClient.OnMessage += HandleMessageAsync;
        _botClient.OnUpdate += HandleUpdateAsync;
        _botClient.OnError += HandleErrorAsync;

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
        finally
        {
            _botClient.OnMessage -= HandleMessageAsync;
            _botClient.OnUpdate -= HandleUpdateAsync;
            _botClient.OnError -= HandleErrorAsync;

            _logger.LogInformation("Telegram bot stopped.");
        }
    }

    private async Task HandleMessageAsync(
        Message message,
        UpdateType updateType)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<TelegramUpdateHandler>();

        await handler.HandleMessageAsync(
            message,
            updateType);
    }

    private async Task HandleUpdateAsync(Update update)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var handler =
            scope.ServiceProvider
                .GetRequiredService<TelegramUpdateHandler>();

        await handler.HandleUpdateAsync(update);
    }

    private Task HandleErrorAsync(
        Exception exception,
        HandleErrorSource source)
    {
        _logger.LogError(
            exception,
            "Telegram polling error. Source: {Source}",
            source);

        return Task.CompletedTask;
    }
}
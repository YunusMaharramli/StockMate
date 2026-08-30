using StockMate.Bot.Telegram.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StockMate.Bot.Telegram.Services;

public class TelegramPollingService : BackgroundService
{
    private static readonly TimeSpan RepeatedWarningInterval = TimeSpan.FromMinutes(1);

    private readonly TelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TelegramPollingService> _logger;
    private long _lastPollingWarningTicks;

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
        User? botInformation = null;
        var failedConnectionAttempts = 0;

        while (!stoppingToken.IsCancellationRequested && botInformation is null)
        {
            try
            {
                botInformation = await _botClient.GetMe(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                failedConnectionAttempts++;

                if (failedConnectionAttempts == 1 || failedConnectionAttempts % 6 == 0)
                {
                    _logger.LogWarning(
                        "Telegram connection is unavailable. Retrying automatically. Reason: {Reason}",
                        exception.Message);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        if (botInformation is null)
            return;

        _logger.LogInformation(
            "Telegram bot @{Username} started.",
            botInformation.Username);

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
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<TelegramUpdateHandler>();
            await handler.HandleMessageAsync(message, updateType);
        }
        catch (OperationCanceledException)
        {
            // Application is stopping.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Telegram message could not be handled.");
            await TrySendErrorAsync(message.Chat.Id);
        }
    }

    private async Task HandleUpdateAsync(Update update)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<TelegramUpdateHandler>();
            await handler.HandleUpdateAsync(update);
        }
        catch (OperationCanceledException)
        {
            // Application is stopping.
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Telegram update could not be handled.");
            if (update.CallbackQuery?.Message is { } message)
                await TrySendErrorAsync(message.Chat.Id);
        }
    }

    private async Task TrySendErrorAsync(long chatId)
    {
        try
        {
            await _botClient.SendMessage(
                chatId,
                "Əməliyyatı yerinə yetirmək mümkün olmadı. " +
                "Məlumatları yoxlayıb yenidən cəhd edin.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Telegram error message could not be sent.");
        }
    }

    private Task HandleErrorAsync(
        Exception exception,
        HandleErrorSource source)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        var lastWarningTicks = Interlocked.Read(ref _lastPollingWarningTicks);

        if (lastWarningTicks == 0 ||
            nowTicks - lastWarningTicks >= RepeatedWarningInterval.Ticks)
        {
            if (Interlocked.CompareExchange(
                    ref _lastPollingWarningTicks,
                    nowTicks,
                    lastWarningTicks) == lastWarningTicks)
            {
                _logger.LogWarning(
                    "Telegram polling is temporarily unavailable. Source: {Source}. Reason: {Reason}",
                    source,
                    exception.Message);
            }
        }

        return Task.CompletedTask;
    }
}

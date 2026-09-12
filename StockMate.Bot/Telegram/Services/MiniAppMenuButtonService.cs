using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace StockMate.Bot.Telegram.Services;

public sealed class MiniAppMenuButtonService
    : IMiniAppMenuButtonService
{
    private readonly TelegramBotClient _botClient;
    private readonly ILogger<MiniAppMenuButtonService> _logger;

    public MiniAppMenuButtonService(
        TelegramBotClient botClient,
        ILogger<MiniAppMenuButtonService> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task ResetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        if (telegramUserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));

        try
        {
            await _botClient.SetChatMenuButton(
                chatId: telegramUserId,
                menuButton: new MenuButtonCommands(),
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            _logger.LogWarning(
                "Telegram menu reset failed for {TelegramUserId}, error code {ErrorCode}.",
                telegramUserId,
                exception.ErrorCode);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning(
                "Telegram network request failed while resetting menu for {TelegramUserId}.",
                telegramUserId);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Telegram menu reset timed out for {TelegramUserId}.",
                telegramUserId);
        }
    }
}

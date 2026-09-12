using Microsoft.Extensions.Options;
using StockMate.Bot.Options;
using StockMate.Bot.Security.Models;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace StockMate.Bot.Telegram.Services;

public sealed class MiniAppMenuButtonService
    : IMiniAppMenuButtonService
{
    private readonly TelegramBotClient _botClient;
    private readonly TelegramOptions _options;
    private readonly ILogger<MiniAppMenuButtonService> _logger;

    public MiniAppMenuButtonService(
        TelegramBotClient botClient,
        IOptions<TelegramOptions> options,
        ILogger<MiniAppMenuButtonService> logger)
    {
        _botClient = botClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SetAsync(
        long telegramUserId,
        MiniAppUserRole role,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_options.MiniAppUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            _logger.LogWarning("Mini App HTTPS URL is not configured; resetting menu for {TelegramUserId}.", telegramUserId);
            await ResetAsync(telegramUserId, cancellationToken);
            return;
        }

        var buttonText = role switch
        {
            MiniAppUserRole.Admin => "📱 Panel",
            MiniAppUserRole.Worker => "📦 Tapşırıqlar",
            MiniAppUserRole.Customer => "🛍 Mağaza",

            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                "Mini App rolu tanınmadı.")
        };

        var menuButton = new MenuButtonWebApp
        {
            Text = buttonText,
            WebApp = new WebAppInfo
            {
                Url = uri.AbsoluteUri
            }
        };

        await UpdateAsync(telegramUserId, menuButton, cancellationToken);
    }

    public async Task ResetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        // Commands explicitly overrides any global Web App menu button.
        await UpdateAsync(telegramUserId, new MenuButtonCommands(), cancellationToken);
    }

    private async Task UpdateAsync(long telegramUserId, MenuButton button, CancellationToken cancellationToken)
    {
        if (telegramUserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));

        try
        {
            await _botClient.SetChatMenuButton(
                chatId: telegramUserId,
                menuButton: button,
                cancellationToken: cancellationToken);
        }
        catch (ApiRequestException exception)
        {
            _logger.LogWarning("Telegram menu update failed for {TelegramUserId}, error code {ErrorCode}.",
                telegramUserId, exception.ErrorCode);
        }
        catch (HttpRequestException)
        {
            _logger.LogWarning("Telegram network request failed while updating menu for {TelegramUserId}.", telegramUserId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Telegram menu update timed out for {TelegramUserId}.", telegramUserId);
        }
    }
}

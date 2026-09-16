using Microsoft.AspNetCore.Mvc;
using StockMate.Application.TelegramInvites.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using Telegram.Bot;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/invites")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppInvitesController : ControllerBase
{
    private readonly ITelegramInviteService _invites;
    private readonly TelegramBotClient _botClient;

    public MiniAppInvitesController(
        ITelegramInviteService invites,
        TelegramBotClient botClient)
    {
        _invites = invites;
        _botClient = botClient;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var bot = await _botClient.GetMe(cancellationToken);

        if (string.IsNullOrWhiteSpace(bot.Username))
            throw new InvalidOperationException(
                "Имя пользователя Telegram-бота недоступно.");

        var token = await _invites.CreateAsync(cancellationToken);
        return Ok(new TelegramInviteLinkResponse(
            $"https://t.me/{bot.Username}?start={token}"));
    }
}

public sealed record TelegramInviteLinkResponse(string Url);

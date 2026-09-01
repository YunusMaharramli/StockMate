using Microsoft.AspNetCore.Mvc;
using StockMate.Application.TelegramInvites.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;
using Telegram.Bot;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/invites")]
public sealed class MiniAppInvitesController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly ITelegramInviteService _invites;
    private readonly TelegramBotClient _botClient;

    public MiniAppInvitesController(
        IMiniAppIdentityService identity,
        ITelegramInviteService invites,
        TelegramBotClient botClient)
    {
        _identity = identity;
        _invites = invites;
        _botClient = botClient;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);

        var bot = await _botClient.GetMe(cancellationToken);

        if (string.IsNullOrWhiteSpace(bot.Username))
            throw new InvalidOperationException(
                "Telegram bot username is not available.");

        var token = await _invites.CreateAsync(cancellationToken);
        return Ok(new TelegramInviteLinkResponse(
            $"https://t.me/{bot.Username}?start={token}"));
    }
}

public sealed record TelegramInviteLinkResponse(string Url);

using Microsoft.AspNetCore.Mvc;
using StockMate.Bot.Security;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/auth")]
public sealed class MiniAppAuthController : ControllerBase
{
    private readonly IMiniAppIdentityService _identityService;

    public MiniAppAuthController(
        IMiniAppIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost]
    public async Task<IActionResult> Authenticate(
        [FromBody] MiniAppAuthRequest request,
        CancellationToken cancellationToken)
    {
        var identity =
            await _identityService.AuthenticateAsync(
                request.InitData,
                cancellationToken);

        return Ok(identity);
    }
}

public sealed record MiniAppAuthRequest(string InitData);
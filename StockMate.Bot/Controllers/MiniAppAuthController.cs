using Microsoft.AspNetCore.Mvc;
using StockMate.Bot.Security;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/auth")]
public class MiniAppAuthController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    public MiniAppAuthController(IMiniAppIdentityService identity) => _identity = identity;

    [HttpPost]
    public async Task<IActionResult> Authenticate(
        [FromBody] MiniAppAuthRequest request,
        CancellationToken cancellationToken)
        => Ok(await _identity.RequireAsync(request.InitData, cancellationToken: cancellationToken));
}

public sealed record MiniAppAuthRequest(string InitData);

using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Shortages.Services;
using StockMate.Application.Staff.Models;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/shortages")]
public sealed class MiniAppShortagesController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IShortageService _shortageService;

    public MiniAppShortagesController(
        IMiniAppIdentityService identity,
        IShortageService shortageService)
    {
        _identity = identity;
        _shortageService = shortageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMissingProducts(
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var products =
            await _shortageService.GetMissingProductsAsync(
                cancellationToken);

        return Ok(products);
    }

    [HttpGet("{productId:guid}/orders")]
    public async Task<IActionResult> GetMissingProductOrders(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var orders =
            await _shortageService.GetMissingProductOrdersAsync(
                productId,
                cancellationToken);

        return Ok(orders);
    }

    private Task<StaffDto> RequireAdminAsync(
        CancellationToken cancellationToken)
    {
        return _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
    }
}
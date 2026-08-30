using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Staff.Models;
using StockMate.Application.StockPurchases.Models;
using StockMate.Application.StockPurchases.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/stock-purchases")]
public sealed class MiniAppStockPurchasesController
    : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IStockPurchaseService
        _stockPurchaseService;

    public MiniAppStockPurchasesController(
        IMiniAppIdentityService identity,
        IStockPurchaseService stockPurchaseService)
    {
        _identity = identity;
        _stockPurchaseService = stockPurchaseService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStockPurchaseRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var purchase =
            await _stockPurchaseService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { purchaseId = purchase.Id },
            purchase);
    }

    [HttpGet("{purchaseId:guid}")]
    public async Task<IActionResult> GetById(
        Guid purchaseId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var purchase =
            await _stockPurchaseService.GetByIdAsync(
                purchaseId,
                cancellationToken);

        return purchase is null
            ? NotFound()
            : Ok(purchase);
    }

    [HttpGet]
    public async Task<IActionResult> GetByPeriod(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var purchases =
            await _stockPurchaseService.GetByPeriodAsync(
                fromUtc,
                toUtc,
                cancellationToken);

        return Ok(purchases);
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
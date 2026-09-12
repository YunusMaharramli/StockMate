using Microsoft.AspNetCore.Mvc;
using StockMate.Application.StockPurchases.Models;
using StockMate.Application.StockPurchases.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/stock-purchases")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppStockPurchasesController
    : ControllerBase
{
    private readonly IStockPurchaseService
        _stockPurchaseService;

    public MiniAppStockPurchasesController(
        IStockPurchaseService stockPurchaseService)
    {
        _stockPurchaseService = stockPurchaseService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStockPurchaseRequest request,
        CancellationToken cancellationToken)
    {
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
        var purchases =
            await _stockPurchaseService.GetByPeriodAsync(
                fromUtc,
                toUtc,
                cancellationToken);

        return Ok(purchases);
    }
}

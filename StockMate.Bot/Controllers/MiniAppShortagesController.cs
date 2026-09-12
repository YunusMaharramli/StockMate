using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Shortages.Exports;
using StockMate.Application.Shortages.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/shortages")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppShortagesController : ControllerBase
{
    private readonly IShortageService _shortageService;
    private readonly IShortageExcelExporter _excelExporter;

    public MiniAppShortagesController(
        IShortageService shortageService,
        IShortageExcelExporter excelExporter)
    {
        _shortageService = shortageService;
        _excelExporter = excelExporter;
    }

    [HttpGet]
    public async Task<IActionResult> GetMissingProducts(
        CancellationToken cancellationToken)
    {
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
        var orders =
            await _shortageService.GetMissingProductOrdersAsync(
                productId,
                cancellationToken);

        return Ok(orders);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        CancellationToken cancellationToken)
    {
        var products =
            await _shortageService.GetMissingProductsAsync(
                cancellationToken);

        var content = _excelExporter.Export(products);

        var fileName =
            $"catismayan-mehsullar-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}

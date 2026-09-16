using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Shortages.Exports;
using StockMate.Application.Shortages.Services;
using StockMate.Bot.Downloads;
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
    private readonly IMiniAppDownloadTicketStore _downloadTickets;

    public MiniAppShortagesController(
        IShortageService shortageService,
        IShortageExcelExporter excelExporter,
        IMiniAppDownloadTicketStore downloadTickets)
    {
        _shortageService = shortageService;
        _excelExporter = excelExporter;
        _downloadTickets = downloadTickets;
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
            $"недостающие-товары-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost("export-ticket")]
    public ActionResult<ShortageExportTicketResponse>
        CreateExportTicket()
    {
        var token = _downloadTickets.Create(
            TimeSpan.FromMinutes(2));

        var fileName =
            $"недостающие-товары-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

        var downloadUrl = Url.ActionLink(
            nameof(MiniAppShortageDownloadsController.Download),
            "MiniAppShortageDownloads",
            new { token },
            Request.Scheme)
            ?? throw new InvalidOperationException(
                "Не удалось создать ссылку для скачивания.");

        return Ok(new ShortageExportTicketResponse(
            downloadUrl,
            fileName));
    }
}

public sealed record ShortageExportTicketResponse(
    string Url,
    string FileName);

using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using StockMate.Application.Shortages.Exports;
using StockMate.Application.Shortages.Services;
using StockMate.Bot.Downloads;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/downloads/shortages")]
public sealed class MiniAppShortageDownloadsController
    : ControllerBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly IMiniAppDownloadTicketStore _downloadTickets;
    private readonly IShortageService _shortages;
    private readonly IShortageExcelExporter _excelExporter;

    public MiniAppShortageDownloadsController(
        IMiniAppDownloadTicketStore downloadTickets,
        IShortageService shortages,
        IShortageExcelExporter excelExporter)
    {
        _downloadTickets = downloadTickets;
        _shortages = shortages;
        _excelExporter = excelExporter;
    }

    [AcceptVerbs("GET", "HEAD")]
    [Route("{token}")]
    public async Task<IActionResult> Download(
        string token,
        CancellationToken cancellationToken)
    {
        if (!_downloadTickets.IsValid(token))
            return NotFound();

        var products = await _shortages.GetMissingProductsAsync(
            cancellationToken);
        var content = _excelExporter.Export(products);
        var fileName =
            $"catismayan-mehsullar-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";

        Response.Headers[HeaderNames.CacheControl] = "no-store";
        Response.Headers[HeaderNames.AccessControlAllowOrigin] =
            "https://web.telegram.org";

        return File(content, ExcelContentType, fileName);
    }
}

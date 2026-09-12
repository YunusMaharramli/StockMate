using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Reports.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/reports")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppReportsController
    : ControllerBase
{
    private readonly IInventoryReportService
        _inventoryReportService;

    public MiniAppReportsController(
        IInventoryReportService inventoryReportService)
    {
        _inventoryReportService = inventoryReportService;
    }

    [HttpGet("inventory-turnover")]
    public async Task<IActionResult> GetInventoryTurnover(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] Guid? deliveryCompanyId,
        CancellationToken cancellationToken)
    {
        var report =
            await _inventoryReportService.GetTurnoverAsync(
                fromUtc,
                toUtc,
                deliveryCompanyId,
                cancellationToken);

        return Ok(report);
    }
}

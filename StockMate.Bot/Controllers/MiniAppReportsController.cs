using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Reports.Services;
using StockMate.Application.Staff.Models;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/reports")]
public sealed class MiniAppReportsController
    : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IInventoryReportService
        _inventoryReportService;

    public MiniAppReportsController(
        IMiniAppIdentityService identity,
        IInventoryReportService inventoryReportService)
    {
        _identity = identity;
        _inventoryReportService = inventoryReportService;
    }

    [HttpGet("inventory-turnover")]
    public async Task<IActionResult> GetInventoryTurnover(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] Guid? deliveryCompanyId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);

        var report =
            await _inventoryReportService.GetTurnoverAsync(
                fromUtc,
                toUtc,
                deliveryCompanyId,
                cancellationToken);

        return Ok(report);
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
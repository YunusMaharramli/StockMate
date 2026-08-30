using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Staff.Models;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/staff")]
public class AdminStaffController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IStaffService _staffService;

    public AdminStaffController(IMiniAppIdentityService identity, IStaffService staffService)
    {
        _identity = identity;
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        return Ok(await _staffService.GetAllAsync(cancellationToken));
    }

    [HttpPost("workers")]
    public async Task<IActionResult> CreateWorker(
        [FromBody] CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        var worker = await _staffService.CreateWorkerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), worker);
    }

    [HttpPut("{staffId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid staffId, CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        await _staffService.ActivateAsync(staffId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{staffId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid staffId, CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        await _staffService.DeactivateAsync(staffId, cancellationToken);
        return NoContent();
    }

    private Task<StaffDto> RequireAdminAsync(CancellationToken cancellationToken)
        => _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
}

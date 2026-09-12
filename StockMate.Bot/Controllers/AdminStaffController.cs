using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Staff.Models;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/staff")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public class AdminStaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public AdminStaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _staffService.GetAllAsync(cancellationToken));
    }

    [HttpPost("workers")]
    public async Task<IActionResult> CreateWorker(
        [FromBody] CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var worker = await _staffService.CreateWorkerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), worker);
    }

    [HttpPut("{staffId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid staffId, CancellationToken cancellationToken)
    {
        await _staffService.ActivateAsync(staffId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{staffId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid staffId, CancellationToken cancellationToken)
    {
        await _staffService.DeactivateAsync(staffId, cancellationToken);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp")]
public class OrderAssignmentsController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IOrderAssignmentService _assignmentService;

    public OrderAssignmentsController(
        IMiniAppIdentityService identity,
        IOrderAssignmentService assignmentService)
    {
        _identity = identity;
        _assignmentService = assignmentService;
    }

    [HttpPost("admin/orders/{orderNumber:long}/assign")]
    public async Task<IActionResult> Assign(
        long orderNumber,
        [FromBody] AssignOrderRequest request,
        CancellationToken cancellationToken)
    {
        var admin = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
        return Ok(await _assignmentService.AssignAsync(
            orderNumber, request.WorkerId, admin.Id, cancellationToken));
    }

    [HttpGet("worker/assignments")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var worker = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Worker,
            cancellationToken);
        return Ok(await _assignmentService.GetWorkerAssignmentsAsync(worker.Id, cancellationToken));
    }

    [HttpGet("admin/orders/{orderNumber:long}/assignment")]
    public async Task<IActionResult> GetForOrder(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
        return Ok(await _assignmentService.GetOrderAssignmentAsync(orderNumber, cancellationToken));
    }

    [HttpDelete("admin/orders/{orderNumber:long}/assignment")]
    public async Task<IActionResult> Release(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var admin = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
        await _assignmentService.ReleaseAsync(orderNumber, admin.Id, cancellationToken);
        return NoContent();
    }
}

public sealed record AssignOrderRequest(Guid WorkerId);

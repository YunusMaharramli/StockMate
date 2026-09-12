using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp")]
public class OrderAssignmentsController : ControllerBase
{
    private readonly IOrderAssignmentService _assignmentService;

    public OrderAssignmentsController(IOrderAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPost("admin/orders/{orderNumber:long}/assign")]
    [MiniAppAuthorize(MiniAppUserRole.Admin)]
    public async Task<IActionResult> Assign(
        long orderNumber,
        [FromBody] AssignOrderRequest request,
        CancellationToken cancellationToken)
    {
        var admin = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _assignmentService.AssignAsync(
            orderNumber, request.WorkerId, admin.StaffId!.Value, cancellationToken));
    }

    [HttpGet("worker/assignments")]
    [MiniAppAuthorize(MiniAppUserRole.Worker)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var worker = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _assignmentService.GetWorkerAssignmentsAsync(
            worker.StaffId!.Value,
            cancellationToken));
    }

    [HttpGet("admin/orders/{orderNumber:long}/assignment")]
    [MiniAppAuthorize(MiniAppUserRole.Admin)]
    public async Task<IActionResult> GetForOrder(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        return Ok(await _assignmentService.GetOrderAssignmentAsync(orderNumber, cancellationToken));
    }

    [HttpDelete("admin/orders/{orderNumber:long}/assignment")]
    [MiniAppAuthorize(MiniAppUserRole.Admin)]
    public async Task<IActionResult> Release(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var admin = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        await _assignmentService.ReleaseAsync(
            orderNumber,
            admin.StaffId!.Value,
            cancellationToken);
        return NoContent();
    }
}

public sealed record AssignOrderRequest(Guid WorkerId);

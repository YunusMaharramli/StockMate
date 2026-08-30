using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Shipments.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp")]
public class ShipmentsController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IMiniAppIdentityService identity, IShipmentService shipmentService)
    {
        _identity = identity;
        _shipmentService = shipmentService;
    }

    [HttpPost("admin/orders/{orderNumber:long}/shipments")]
    public async Task<IActionResult> Create(
        long orderNumber,
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var admin = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
        var shipment = await _shipmentService.CreateReadyShipmentAsync(
            orderNumber, request.WorkerId, admin.Id, cancellationToken);
        return Ok(shipment);
    }

    [HttpGet("worker/shipments")]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var worker = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Worker,
            cancellationToken);
        return Ok(await _shipmentService.GetWorkerShipmentsAsync(worker.Id, cancellationToken));
    }

    [HttpGet("admin/orders/{orderNumber:long}/shipments")]
    public async Task<IActionResult> GetForOrder(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
        return Ok(await _shipmentService.GetOrderShipmentsAsync(orderNumber, cancellationToken));
    }

    [HttpPut("shipments/{shipmentId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid shipmentId,
        [FromBody] ChangeShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var staff = await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            cancellationToken: cancellationToken);
        return Ok(await _shipmentService.ChangeStatusAsync(
            shipmentId, staff.Id, request.Status, cancellationToken));
    }
}

public sealed record CreateShipmentRequest(Guid WorkerId);
public sealed record ChangeShipmentStatusRequest(ShipmentStatus Status);

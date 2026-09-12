using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Shipments.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipmentService;

    public ShipmentsController(IShipmentService shipmentService)
    {
        _shipmentService = shipmentService;
    }

    [HttpPost("admin/orders/{orderNumber:long}/shipments")]
    [MiniAppAuthorize(MiniAppUserRole.Admin)]
    public async Task<IActionResult> Create(
        long orderNumber,
        [FromBody] CreateShipmentRequest request,
        CancellationToken cancellationToken)
    {
        var admin = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var shipment = await _shipmentService.CreateReadyShipmentAsync(
            orderNumber, request.WorkerId, admin.StaffId!.Value, cancellationToken);
        return Ok(shipment);
    }

    [HttpGet("worker/shipments")]
    [MiniAppAuthorize(MiniAppUserRole.Worker)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var worker = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _shipmentService.GetWorkerShipmentsAsync(
            worker.StaffId!.Value,
            cancellationToken));
    }

    [HttpGet("admin/orders/{orderNumber:long}/shipments")]
    [MiniAppAuthorize(MiniAppUserRole.Admin)]
    public async Task<IActionResult> GetForOrder(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        return Ok(await _shipmentService.GetOrderShipmentsAsync(orderNumber, cancellationToken));
    }

    [HttpPut("shipments/{shipmentId:guid}/status")]
    [MiniAppAuthorize(MiniAppUserRole.Admin, MiniAppUserRole.Worker)]
    public async Task<IActionResult> ChangeStatus(
        Guid shipmentId,
        [FromBody] ChangeShipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var staff = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _shipmentService.ChangeStatusAsync(
            shipmentId, staff.StaffId!.Value, request.Status, cancellationToken));
    }
}

public sealed record CreateShipmentRequest(Guid WorkerId);
public sealed record ChangeShipmentStatusRequest(ShipmentStatus Status);

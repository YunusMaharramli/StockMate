using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/orders")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppAdminOperationsController : ControllerBase
{
    private readonly IOrderService _orders;

    public MiniAppAdminOperationsController(
        IOrderService orders)
    {
        _orders = orders;
    }

    [HttpPut("{orderNumber:long}/delivering")]
    public async Task<IActionResult> MarkDelivering(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orders.MarkAsDeliveringAsync(orderNumber, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/complete")]
    public async Task<IActionResult> Complete(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orders.CompleteAsync(orderNumber, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/deliver-ready")]
    public async Task<IActionResult> DeliverReady(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.DeliverReadyItemsAsync(orderNumber, cancellationToken));
    }

    [HttpPut("{orderNumber:long}/delivery-fee")]
    public async Task<IActionResult> SetDeliveryFee(
        long orderNumber,
        [FromBody] MiniAppDeliveryFeeRequest request,
        CancellationToken cancellationToken)
    {
        await _orders.SetDeliveryFeeAsync(orderNumber, request.DeliveryFee, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/admin-note")]
    public async Task<IActionResult> SetAdminNote(
        long orderNumber,
        [FromBody] MiniAppAdminNoteRequest request,
        CancellationToken cancellationToken)
    {
        await _orders.SetAdminNoteAsync(orderNumber, request.AdminNote, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/items/{orderItemId:guid}/special-status")]
    public async Task<IActionResult> ChangeSpecialOrderStatus(
        long orderNumber,
        Guid orderItemId,
        [FromBody] MiniAppSpecialOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _orders.ChangeSpecialOrderStatusAsync(
            orderNumber,
            orderItemId,
            request.Status,
            cancellationToken);
        return NoContent();
    }

}

public sealed record MiniAppDeliveryFeeRequest(decimal DeliveryFee);
public sealed record MiniAppAdminNoteRequest(string? AdminNote);
public sealed record MiniAppSpecialOrderStatusRequest(SpecialOrderStatus Status);

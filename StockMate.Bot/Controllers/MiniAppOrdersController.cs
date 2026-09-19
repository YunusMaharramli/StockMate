using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/orders")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public class MiniAppOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public MiniAppOrdersController(
    IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("new")]
    public async Task<IActionResult> GetNew(CancellationToken cancellationToken)
    {
        return Ok(await _orderService.GetNewOrdersAsync(cancellationToken));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _orderService.GetActiveOrdersAsync(cancellationToken));
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(CancellationToken cancellationToken)
    {
        return Ok(await _orderService.GetOrderHistoryAsync(cancellationToken));
    }

    [HttpGet("{orderNumber:long}")]
    public async Task<IActionResult> GetByNumber(long orderNumber, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPut("{orderNumber:long}/confirm")]
    public async Task<IActionResult> Confirm(
    long orderNumber,
    CancellationToken cancellationToken)
    {
        await _orderService.ConfirmAsync(
            orderNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{orderNumber:long}/preparing")]
    public async Task<IActionResult> MarkPreparing(long orderNumber, CancellationToken cancellationToken)
    {
        await _orderService.MarkAsPreparingAsync(orderNumber, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/delivery-mode")]
    public async Task<IActionResult> SetDeliveryMode(
        long orderNumber,
        [FromBody] SetOrderDeliveryModeRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.SetDeliveryModeAsync(orderNumber, request.Mode, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long orderNumber,
        [FromBody] MiniAppCancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.CancelAsync(orderNumber, request.Reason, cancellationToken);
        return NoContent();
    }

    [HttpPut("{orderNumber:long}/cancel-remaining")]
    public async Task<IActionResult> CancelRemaining(long orderNumber, CancellationToken cancellationToken)
    {
        return Ok(await _orderService.CancelRemainingItemsAsync(orderNumber, cancellationToken));
    }

}

public sealed record SetOrderDeliveryModeRequest(DeliveryMode Mode);
public sealed record MiniAppCancelOrderRequest(string Reason);

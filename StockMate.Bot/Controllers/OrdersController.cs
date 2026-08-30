using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Orders.Models;
using StockMate.Application.Orders.Services;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("checkout/{telegramUserId:long}")]
    public async Task<ActionResult<OrderDto>> Checkout(
        long telegramUserId,
        [FromBody] CheckoutOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CheckoutAsync(
            telegramUserId,
            request.CustomerAddressId,
            request.DeliveryCompanyId,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByOrderNumber),
            new { orderNumber = order.OrderNumber },
            order);
    }

    [HttpGet("{orderNumber:long}")]
    public async Task<ActionResult<OrderDto>> GetByOrderNumber(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByOrderNumberAsync(
            orderNumber,
            cancellationToken);

        if (order is null)
            return NotFound();

        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetByStatus(
        [FromQuery] OrderStatus status,
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetByStatusAsync(
            status,
            cancellationToken);

        return Ok(orders);
    }

    [HttpPost("{orderNumber:long}/confirm")]
    public async Task<IActionResult> Confirm(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orderService.ConfirmAsync(
            orderNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{orderNumber:long}/preparing")]
    public async Task<IActionResult> MarkAsPreparing(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orderService.MarkAsPreparingAsync(
            orderNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{orderNumber:long}/delivering")]
    public async Task<IActionResult> MarkAsDelivering(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orderService.MarkAsDeliveringAsync(
            orderNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{orderNumber:long}/complete")]
    public async Task<IActionResult> Complete(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        await _orderService.CompleteAsync(
            orderNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{orderNumber:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long orderNumber,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.CancelAsync(
            orderNumber,
            request.Reason,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{orderNumber:long}/delivery-fee")]
    public async Task<IActionResult> SetDeliveryFee(
        long orderNumber,
        [FromBody] SetDeliveryFeeRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.SetDeliveryFeeAsync(
            orderNumber,
            request.DeliveryFee,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{orderNumber:long}/admin-note")]
    public async Task<IActionResult> SetAdminNote(
        long orderNumber,
        [FromBody] SetAdminNoteRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.SetAdminNoteAsync(
            orderNumber,
            request.AdminNote,
            cancellationToken);

        return NoContent();
    }

    [HttpPut(
        "{orderNumber:long}/items/{orderItemId:guid}/special-status")]
    public async Task<IActionResult> ChangeSpecialOrderStatus(
        long orderNumber,
        Guid orderItemId,
        [FromBody] ChangeSpecialOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _orderService.ChangeSpecialOrderStatusAsync(
            orderNumber,
            orderItemId,
            request.Status,
            cancellationToken);

        return NoContent();
    }
    [HttpGet("new")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetNewOrders(
    CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetNewOrdersAsync(
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetActiveOrders(
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetActiveOrdersAsync(
            cancellationToken);

        return Ok(orders);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrderHistory(
        CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetOrderHistoryAsync(
            cancellationToken);

        return Ok(orders);
    }
}

public sealed record CancelOrderRequest(string Reason);

public sealed record CheckoutOrderRequest(
    Guid CustomerAddressId,
    Guid DeliveryCompanyId);

public sealed record SetAdminNoteRequest(string? AdminNote);

public sealed record ChangeSpecialOrderStatusRequest(
    SpecialOrderStatus Status);

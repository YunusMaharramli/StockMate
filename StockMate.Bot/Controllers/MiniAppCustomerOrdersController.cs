using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Notifications;
using StockMate.Application.Orders.Models;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Controllers.Models;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/customer/orders")]
[MiniAppAuthorize(MiniAppUserRole.Customer)]
public sealed class MiniAppCustomerOrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly IWorkflowNotificationService _notifications;

    public MiniAppCustomerOrdersController(
        IOrderService orders,
        IWorkflowNotificationService notifications)
    {
        _orders = orders;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerOrderResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var orders = await _orders.GetCustomerOrdersAsync(
            customer.TelegramUserId,
            cancellationToken);

        return Ok(orders.Select(Map).ToList());
    }

    [HttpGet("{orderNumber:long}")]
    public async Task<ActionResult<CustomerOrderResponse>> GetByNumber(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var order = await GetOwnedOrderAsync(customer, orderNumber, cancellationToken);
        return Ok(Map(order));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CustomerOrderResponse>> Checkout(
        [FromBody] CustomerCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var order = await _orders.CheckoutAsync(
            customer.TelegramUserId,
            request.CustomerAddressId,
            request.DeliveryCompanyId,
            cancellationToken);

        await _notifications.NotifyOrderCreatedAsync(
            order.OrderNumber,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByNumber),
            new { orderNumber = order.OrderNumber },
            Map(order));
    }

    [HttpPost("{orderNumber:long}/cancel")]
    public async Task<IActionResult> Cancel(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var order = await GetOwnedOrderAsync(customer, orderNumber, cancellationToken);

        if (order.Status != OrderStatus.PendingAdminApproval)
        {
            throw new InvalidOperationException(
                "Yalnız admin təsdiqindən əvvəl sifarişi ləğv etmək olar.");
        }

        await _orders.CancelAsync(
            orderNumber,
            "Müştəri tərəfindən ləğv edildi.",
            cancellationToken);

        return NoContent();
    }

    private async Task<OrderDto> GetOwnedOrderAsync(
        MiniAppIdentity customer,
        long orderNumber,
        CancellationToken cancellationToken)
    {
        var order = await _orders.GetByOrderNumberAsync(
            orderNumber,
            cancellationToken)
            ?? throw new KeyNotFoundException("Sifariş tapılmadı.");

        if (!customer.CustomerId.HasValue ||
            order.CustomerId != customer.CustomerId.Value)
        {
            throw new UnauthorizedAccessException("İcazəniz yoxdur.");
        }

        return order;
    }

    private static CustomerOrderResponse Map(OrderDto order)
    {
        return new CustomerOrderResponse(
            order.OrderNumber,
            order.PhoneNumber,
            order.DeliveryAddress,
            order.DeliveryCompanyName,
            order.Status,
            order.Items.Select(item => new CustomerOrderItemResponse(
                item.ProductId,
                item.ProductName,
                item.ProductCode,
                item.UnitPrice,
                item.TotalQuantity,
                item.LineTotal)).ToList(),
            order.ProductsTotal,
            order.DeliveryFee,
            order.TotalAmount,
            order.CancellationReason,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.CompletedAtUtc,
            order.CancelledAtUtc);
    }
}

using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Common.Idempotency;
using StockMate.Application.Common.Outbox;
using StockMate.Application.Notifications;
using StockMate.Application.Orders.Models;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Controllers.Models;
using StockMate.Bot.Extensions;
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
    private readonly IOutboxWriter _outbox;
    private readonly IIdempotencyService _idempotency;

    public MiniAppCustomerOrdersController(
    IOrderService orders,
    IOutboxWriter outbox,
    IIdempotencyService idempotency)
    {
        _orders = orders;
        _outbox = outbox;
        _idempotency = idempotency;
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
        var customer =
            MiniAppAuthorizationFilter.GetIdentity(HttpContext);

        var operationId =
            Request.GetRequiredOperationId();

        var response = await _idempotency.ExecuteAsync(
            operationId,
            customer.TelegramUserId,
            IdempotencyOperationTypes.CustomerCheckout,
            request,
            async token =>
            {
                var order = await _orders.CheckoutAsync(
                    customer.TelegramUserId,
                    request.CustomerAddressId,
                    request.DeliveryCompanyId,
                    token);

                await _outbox.EnqueueAsync(
                        OutboxMessageTypes.OrderCreated,
                        new OrderCreatedOutboxPayload(order.OrderNumber),
                        token);

                return Map(order);
            },
            cancellationToken);

        return Ok(response);
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
                "Заказ можно отменить только до подтверждения администратором.");
        }

        await _orders.CancelAsync(
            orderNumber,
            "Отменено клиентом.",
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
            ?? throw new KeyNotFoundException("Заказ не найден.");

        if (!customer.CustomerId.HasValue ||
            order.CustomerId != customer.CustomerId.Value)
        {
            throw new UnauthorizedAccessException("У вас нет доступа.");
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

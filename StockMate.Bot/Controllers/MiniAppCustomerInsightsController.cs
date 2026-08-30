using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Services;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/customers")]
public sealed class MiniAppCustomerInsightsController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly ICustomerService _customers;
    private readonly IOrderService _orders;

    public MiniAppCustomerInsightsController(
        IMiniAppIdentityService identity,
        ICustomerService customers,
        IOrderService orders)
    {
        _identity = identity;
        _customers = customers;
        _orders = orders;
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("{customerId:guid}/orders")]
    public async Task<IActionResult> GetOrders(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        return Ok(await _orders.GetCustomerOrdersByCustomerIdAsync(
            customerId,
            cancellationToken));
    }

    private async Task RequireAdminAsync(CancellationToken cancellationToken)
        => await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
}

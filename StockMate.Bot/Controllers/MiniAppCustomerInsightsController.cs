using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Services;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/customers")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppCustomerInsightsController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly IOrderService _orders;

    public MiniAppCustomerInsightsController(
        ICustomerService customers,
        IOrderService orders)
    {
        _customers = customers;
        _orders = orders;
    }

    [HttpGet("{customerId:guid}")]
    public async Task<IActionResult> Get(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(customerId, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpGet("{customerId:guid}/orders")]
    public async Task<IActionResult> GetOrders(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetCustomerOrdersByCustomerIdAsync(
            customerId,
            cancellationToken));
    }

    [HttpPut("{customerId:guid}/block")]
    public async Task<IActionResult> Block(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        await _customers.BlockAsync(customerId, cancellationToken);
        return NoContent();
    }
}

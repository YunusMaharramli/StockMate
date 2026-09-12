using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Carts.Models;
using StockMate.Application.Carts.Services;
using StockMate.Bot.Controllers.Models;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/customer/cart")]
[MiniAppAuthorize(MiniAppUserRole.Customer)]
public sealed class MiniAppCustomerCartController : ControllerBase
{
    private readonly ICartService _cart;

    public MiniAppCustomerCartController(ICartService cart)
    {
        _cart = cart;
    }

    [HttpGet]
    public async Task<ActionResult<CustomerCartResponse>> Get(
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(Map(await _cart.GetAsync(customer.TelegramUserId, cancellationToken)));
    }

    [HttpPut("items")]
    public async Task<ActionResult<CustomerCartResponse>> SetItem(
        [FromBody] SetCustomerCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var cart = await _cart.AddOrUpdateItemAsync(
            customer.TelegramUserId,
            new AddCartItemRequest(
                request.ProductId,
                request.Quantity,
                CartItemFulfillmentMode.IncludeSpecialOrder),
            cancellationToken);

        return Ok(Map(cart));
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CustomerCartResponse>> RemoveItem(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(Map(await _cart.RemoveItemAsync(
            customer.TelegramUserId,
            productId,
            cancellationToken)));
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken)
    {
        var customer = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        await _cart.ClearAsync(customer.TelegramUserId, cancellationToken);
        return NoContent();
    }

    private static CustomerCartResponse Map(CartDto cart)
    {
        return new CustomerCartResponse(
            cart.Id,
            cart.Items.Select(item => new CustomerCartItemResponse(
                item.ProductId,
                item.ProductName,
                item.ProductCode,
                item.UnitPrice,
                item.RequestedQuantity,
                item.LineTotal)).ToList(),
            cart.ProductsTotal,
            cart.ExpiresAtUtc);
    }
}

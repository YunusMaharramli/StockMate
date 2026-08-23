using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Carts.Models;
using StockMate.Application.Carts.Services;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/carts/{telegramUserId:long}")]
public class CartsController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartsController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> Get(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetAsync(
            telegramUserId,
            cancellationToken);

        return Ok(cart);
    }

    [HttpPut("items")]
    public async Task<ActionResult<CartDto>> AddOrUpdateItem(
        long telegramUserId,
        [FromBody] AddCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.AddOrUpdateItemAsync(
            telegramUserId,
            request,
            cancellationToken);

        return Ok(cart);
    }

    [HttpDelete("items/{productId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(
        long telegramUserId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.RemoveItemAsync(
            telegramUserId,
            productId,
            cancellationToken);

        return Ok(cart);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        await _cartService.ClearAsync(
            telegramUserId,
            cancellationToken);

        return NoContent();
    }
}
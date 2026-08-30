using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Services;
using StockMate.Application.Products.Models;
using StockMate.Application.Products.Services;
using StockMate.Application.Staff.Models;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin")]
public class MiniAppCatalogController : ControllerBase
{
    private readonly IMiniAppIdentityService _identity;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;

    public MiniAppCatalogController(
        IMiniAppIdentityService identity,
        IProductService productService,
        ICustomerService customerService)
    {
        _identity = identity;
        _productService = productService;
        _customerService = customerService;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        return Ok(await _productService.GetAdminPageAsync(
            pageNumber, pageSize, search, cancellationToken));
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        var product = await _productService.CreateAsync(request, cancellationToken);
        return Created($"api/miniapp/admin/products/{product.Id}", product);
    }

    [HttpPut("products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        await _productService.UpdateDetailsAsync(productId, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("products/{productId:guid}/price")]
    public async Task<IActionResult> ChangePrice(
        Guid productId,
        [FromBody] MiniAppPriceRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        await _productService.ChangePriceAsync(productId, request.Price, cancellationToken);
        return NoContent();
    }

    [HttpPut("products/{productId:guid}/active")]
    public async Task<IActionResult> SetProductActive(
        Guid productId,
        [FromBody] MiniAppActiveRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        if (request.IsActive)
            await _productService.ActivateAsync(productId, cancellationToken);
        else
            await _productService.DeactivateAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/{productId:guid}/stock")]
    public async Task<IActionResult> ChangeStock(
        Guid productId,
        [FromBody] MiniAppStockRequest request,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        switch (request.Action.Trim().ToLowerInvariant())
        {
            case "add":
                await _productService.AddStockAsync(productId, request.Quantity, request.Reason, cancellationToken);
                break;
            case "remove":
                await _productService.RemoveStockAsync(productId, request.Quantity, request.Reason, cancellationToken);
                break;
            case "set":
                await _productService.SetStockAsync(productId, request.Quantity, request.Reason, cancellationToken);
                break;
            default:
                throw new ArgumentException("Stock action must be add, remove or set.");
        }
        return NoContent();
    }

    [HttpGet("products/{productId:guid}/stock-history")]
    public async Task<IActionResult> GetStockHistory(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        return Ok(await _productService.GetStockHistoryAsync(productId, cancellationToken));
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        await RequireAdminAsync(cancellationToken);
        return Ok(await _customerService.GetAllAsync(cancellationToken));
    }

    private Task<StaffDto> RequireAdminAsync(CancellationToken cancellationToken)
        => _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
}

public sealed record MiniAppPriceRequest(decimal Price);
public sealed record MiniAppActiveRequest(bool IsActive);
public sealed record MiniAppStockRequest(string Action, int Quantity, string Reason);

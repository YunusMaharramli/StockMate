using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Services;
using StockMate.Application.Products.Models;
using StockMate.Application.Products.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public class MiniAppCatalogController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;

    public MiniAppCatalogController(
        IProductService productService,
        ICustomerService customerService)
    {
        _productService = productService;
        _customerService = customerService;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? subcategoryId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? inStock = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _productService.GetAdminPageAsync(
            new AdminProductQuery(
                pageNumber,
                pageSize,
                search,
                categoryId,
                subcategoryId,
                isActive,
                inStock,
                lowStock,
                includeArchived),
            cancellationToken));
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {

        if (!request.CategoryId.HasValue)
            throw new ArgumentException("Category ID is required.", nameof(request));

        var product = await _productService.CreateAsync(request, cancellationToken);
        return Created($"api/miniapp/admin/products/{product.Id}", product);
    }

    [HttpPut("products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.UpdateDetailsAsync(productId, request, cancellationToken);
        return NoContent();
    }

    [HttpPatch("products/{productId:guid}/price")]
    public async Task<IActionResult> ChangePrice(
        Guid productId,
        [FromBody] MiniAppPriceRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.ChangePriceAsync(productId, request.Price, cancellationToken);
        return NoContent();
    }

    [HttpPut("products/{productId:guid}/active")]
    public async Task<IActionResult> SetProductActive(
        Guid productId,
        [FromBody] MiniAppActiveRequest request,
        CancellationToken cancellationToken)
    {
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
        return Ok(await _productService.GetStockHistoryAsync(productId, cancellationToken));
    }

    [HttpPost("products/{productId:guid}/archive")]
    public async Task<IActionResult> ArchiveProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await _productService.ArchiveAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/{productId:guid}/restore")]
    public async Task<IActionResult> RestoreProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await _productService.RestoreAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/bulk")]
    public async Task<IActionResult> ApplyBulkProductAction(
        [FromBody] BulkProductActionRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.ApplyBulkActionAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers(CancellationToken cancellationToken)
    {
        return Ok(await _customerService.GetAllAsync(cancellationToken));
    }

}

public sealed record MiniAppPriceRequest(decimal Price);
public sealed record MiniAppActiveRequest(bool IsActive);
public sealed record MiniAppStockRequest(string Action, int Quantity, string Reason);

using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Products.Models;
using StockMate.Application.Products.Services;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = product.Id },
            product);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
            return NotFound();

        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetActivePage(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? subcategoryName = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetCatalogPageAsync(
            new ProductCatalogQuery(
                pageNumber,
                pageSize,
                categoryId,
                subcategoryName,
                search),
            cancellationToken);

        return Ok(result);
    }

    [HttpPatch("{id:guid}/price")]
    public async Task<IActionResult> ChangePrice(
        Guid id,
        [FromBody] ChangePriceRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.ChangePriceAsync(
            id,
            request.Price,
            cancellationToken);

        return NoContent();
    }
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDetails(
    Guid id,
    [FromBody] UpdateProductRequest request,
    CancellationToken cancellationToken)
    {
        await _productService.UpdateDetailsAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/stock/add")]
    public async Task<IActionResult> AddStock(
        Guid id,
        [FromBody] ChangeStockRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.AddStockAsync(
            id,
            request.Quantity,
            request.Reason,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/stock/remove")]
    public async Task<IActionResult> RemoveStock(
        Guid id,
        [FromBody] ChangeStockRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.RemoveStockAsync(
            id,
            request.Quantity,
            request.Reason,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/stock")]
    public async Task<IActionResult> SetStock(
        Guid id,
        [FromBody] ChangeStockRequest request,
        CancellationToken cancellationToken)
    {
        await _productService.SetStockAsync(
            id,
            request.Quantity,
            request.Reason,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.ActivateAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _productService.DeactivateAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/stock-history")]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>>
        GetStockHistory(
            Guid id,
            CancellationToken cancellationToken)
    {
        var history =
            await _productService.GetStockHistoryAsync(
                id,
                cancellationToken);

        return Ok(history);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetAdminPage(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 5,
    [FromQuery] string? search = null,
    CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetAdminPageAsync(
            pageNumber,
            pageSize,
            search,
            cancellationToken);

        return Ok(result);
    }
}

public sealed record ChangePriceRequest(decimal Price);

public sealed record ChangeStockRequest(
    int Quantity,
    string Reason);

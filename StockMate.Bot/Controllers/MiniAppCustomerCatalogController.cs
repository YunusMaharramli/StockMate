using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Categories.Models;
using StockMate.Application.Categories.Services;
using StockMate.Application.Products.Models;
using StockMate.Application.Products.Services;
using StockMate.Application.Subcategories.Models;
using StockMate.Application.Subcategories.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/customer/catalog")]
[MiniAppAuthorize(MiniAppUserRole.Customer)]
public sealed class MiniAppCustomerCatalogController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly ISubcategoryService _subcategories;

    public MiniAppCustomerCatalogController(
        IProductService products,
        ICategoryService categories,
        ISubcategoryService subcategories)
    {
        _products = products;
        _categories = categories;
        _subcategories = subcategories;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductCatalogItemDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? subcategoryName = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {

        return Ok(await _products.GetCatalogPageAsync(
            new ProductCatalogQuery(
                pageNumber,
                pageSize,
                categoryId,
                subcategoryName,
                search),
            cancellationToken));
    }

    [HttpGet("products/{productId:guid}")]
    public async Task<ActionResult<ProductCatalogItemDto>> GetProduct(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetCatalogProductByIdAsync(
            productId,
            cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        return Ok(await _categories.GetActiveAsync(cancellationToken));
    }

    [HttpGet("subcategories")]
    public async Task<ActionResult<IReadOnlyList<SubcategoryDto>>> GetSubcategories(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        return Ok(await _subcategories.GetActiveAsync(categoryId, cancellationToken));
    }

}

using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Categories.Models;
using StockMate.Application.Categories.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/categories")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppCategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public MiniAppCategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetAllAsync(
            includeArchived,
            cancellationToken);

        return Ok(categories);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetActive(
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetActiveAsync(
            cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { categoryId = category.Id },
            category);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<IActionResult> Update(
        Guid categoryId,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.UpdateAsync(
            categoryId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{categoryId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.SetActiveAsync(
            categoryId,
            true,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{categoryId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.SetActiveAsync(
            categoryId,
            false,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{categoryId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.ArchiveAsync(
            categoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{categoryId:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.RestoreAsync(
            categoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> DeleteEmpty(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.DeleteEmptyAsync(
            categoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{categoryId:guid}/remove")]
    public async Task<IActionResult> Remove(
        Guid categoryId,
        [FromBody] RemoveCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await _categoryService.RemoveAsync(categoryId, request, cancellationToken);
        return NoContent();
    }

}

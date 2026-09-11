using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Subcategories.Models;
using StockMate.Application.Subcategories.Services;
using StockMate.Application.Staff.Models;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/subcategories")]
public sealed class MiniAppSubcategoriesController : ControllerBase
{
    private readonly ISubcategoryService _subcategoryService;
    private readonly IMiniAppIdentityService _identity;

    public MiniAppSubcategoriesController(
        ISubcategoryService subcategoryService,
        IMiniAppIdentityService identity)
    {
        _subcategoryService = subcategoryService;
        _identity = identity;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubcategoryDto>>> GetAll(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        var subcategories = await _subcategoryService.GetAllAsync(
            categoryId,
            includeArchived,
            cancellationToken);

        return Ok(subcategories);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<SubcategoryDto>>> GetActive(
        [FromQuery] Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        var subcategories = await _subcategoryService.GetActiveAsync(
            categoryId,
            cancellationToken);

        return Ok(subcategories);
    }

    [HttpGet("{subcategoryId:guid}")]
    public async Task<ActionResult<SubcategoryDto>> GetById(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        var subcategory = await _subcategoryService.GetByIdAsync(
            subcategoryId,
            cancellationToken);

        if (subcategory is null)
        {
            return NotFound();
        }

        return Ok(subcategory);
    }

    [HttpPost]
    public async Task<ActionResult<SubcategoryDto>> Create(
        [FromBody] CreateSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        var subcategory = await _subcategoryService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { subcategoryId = subcategory.Id },
            subcategory);
    }

    [HttpPut("{subcategoryId:guid}")]
    public async Task<IActionResult> Update(
        Guid subcategoryId,
        [FromBody] UpdateSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.UpdateAsync(
            subcategoryId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{subcategoryId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.SetActiveAsync(
            subcategoryId,
            true,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{subcategoryId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.SetActiveAsync(
            subcategoryId,
            false,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{subcategoryId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.ArchiveAsync(
            subcategoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{subcategoryId:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.RestoreAsync(
            subcategoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{subcategoryId:guid}")]
    public async Task<IActionResult> DeleteEmpty(
        Guid subcategoryId,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.DeleteEmptyAsync(
            subcategoryId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{subcategoryId:guid}/remove")]
    public async Task<IActionResult> Remove(
        Guid subcategoryId,
        [FromBody] RemoveSubcategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireAdminAsync(cancellationToken);
        await _subcategoryService.RemoveAsync(subcategoryId, request, cancellationToken);
        return NoContent();
    }

    private Task<StaffDto> RequireAdminAsync(CancellationToken cancellationToken)
        => _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);
}

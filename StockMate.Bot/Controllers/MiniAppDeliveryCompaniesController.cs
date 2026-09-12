using Microsoft.AspNetCore.Mvc;
using StockMate.Application.DeliveryCompanies.Models;
using StockMate.Application.DeliveryCompanies.Services;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/admin/delivery-companies")]
[MiniAppAuthorize(MiniAppUserRole.Admin)]
public sealed class MiniAppDeliveryCompaniesController
    : ControllerBase
{
    private readonly IDeliveryCompanyService
        _deliveryCompanyService;

    public MiniAppDeliveryCompaniesController(
        IDeliveryCompanyService deliveryCompanyService)
    {
        _deliveryCompanyService = deliveryCompanyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken cancellationToken)
    {
        var companies =
            await _deliveryCompanyService.GetAllAsync(
                cancellationToken);

        return Ok(companies);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeliveryCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var company =
            await _deliveryCompanyService.CreateAsync(
                request,
                cancellationToken);

        return Created(
            $"/api/miniapp/admin/delivery-companies/{company.Id}",
            company);
    }

    [HttpPut("{companyId:guid}/name")]
    public async Task<IActionResult> Rename(
        Guid companyId,
        [FromBody] RenameDeliveryCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var company =
            await _deliveryCompanyService.RenameAsync(
                companyId,
                request,
                cancellationToken);

        return Ok(company);
    }

    [HttpPut("{companyId:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        await _deliveryCompanyService.SetActiveAsync(
            companyId,
            true,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{companyId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        await _deliveryCompanyService.SetActiveAsync(
            companyId,
            false,
            cancellationToken);

        return NoContent();
    }
}

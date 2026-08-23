using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Models;
using StockMate.Application.Customers.Services;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost("telegram")]
    public async Task<ActionResult<CustomerDto>> CreateOrUpdate(
        [FromBody] UpsertCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _customerService.CreateOrUpdateAsync(
            request,
            cancellationToken);

        return Ok(customer);
    }

    [HttpGet("telegram/{telegramUserId:long}")]
    public async Task<ActionResult<CustomerDto>> GetByTelegramUserId(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId,
                cancellationToken);

        if (customer is null)
            return NotFound();

        return Ok(customer);
    }

    [HttpPut("telegram/{telegramUserId:long}/contact")]
    public async Task<IActionResult> UpdateContactInformation(
        long telegramUserId,
        [FromBody] UpdateContactInformationRequest request,
        CancellationToken cancellationToken)
    {
        await _customerService.UpdateContactInformationAsync(
            telegramUserId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPut("telegram/{telegramUserId:long}/page-size")]
    public async Task<IActionResult> ChangeProductPageSize(
        long telegramUserId,
        [FromBody] ChangePageSizeRequest request,
        CancellationToken cancellationToken)
    {
        await _customerService.ChangeProductPageSizeAsync(
            telegramUserId,
            request.PageSize,
            cancellationToken);

        return NoContent();
    }
}

public sealed record ChangePageSizeRequest(int PageSize);
using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Customers.Models;
using StockMate.Application.Customers.Services;
using StockMate.Application.DeliveryCompanies.Models;
using StockMate.Application.DeliveryCompanies.Services;
using StockMate.Bot.Controllers.Models;
using StockMate.Bot.Security;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Controllers;

[ApiController]
[Route("api/miniapp/customer/profile")]
[MiniAppAuthorize(MiniAppUserRole.Customer)]
public sealed class MiniAppCustomerProfileController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly ICustomerAddressService _addresses;
    private readonly IDeliveryCompanyService _deliveryCompanies;

    public MiniAppCustomerProfileController(
        ICustomerService customers,
        ICustomerAddressService addresses,
        IDeliveryCompanyService deliveryCompanies)
    {
        _customers = customers;
        _addresses = addresses;
        _deliveryCompanies = deliveryCompanies;
    }

    [HttpGet]
    public async Task<ActionResult<CustomerProfileResponse>> Get(
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var customer = await _customers.GetByTelegramUserIdAsync(
            identity.TelegramUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Müştəri tapılmadı.");

        return Ok(new CustomerProfileResponse(
            customer.FirstName,
            customer.LastName,
            customer.Username,
            customer.PhoneNumber));
    }

    [HttpPut("phone")]
    public async Task<IActionResult> UpdatePhone(
        [FromBody] UpdateCustomerPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        await _customers.UpdatePhoneNumberAsync(
            identity.TelegramUserId,
            request.PhoneNumber,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressDto>>> GetAddresses(
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _addresses.GetActiveAsync(
            identity.TelegramUserId,
            cancellationToken));
    }

    [HttpPost("addresses")]
    public async Task<ActionResult<CustomerAddressDto>> CreateAddress(
        [FromBody] CreateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        var address = await _addresses.CreateAsync(
            identity.TelegramUserId,
            request,
            cancellationToken);

        return CreatedAtAction(nameof(GetAddresses), address);
    }

    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<CustomerAddressDto>> UpdateAddress(
        Guid addressId,
        [FromBody] UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        return Ok(await _addresses.UpdateAsync(
            identity.TelegramUserId,
            addressId,
            request,
            cancellationToken));
    }

    [HttpDelete("addresses/{addressId:guid}")]
    public async Task<IActionResult> DeleteAddress(
        Guid addressId,
        CancellationToken cancellationToken)
    {
        var identity = MiniAppAuthorizationFilter.GetIdentity(HttpContext);
        await _addresses.DeactivateAsync(
            identity.TelegramUserId,
            addressId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("delivery-companies")]
    public async Task<ActionResult<IReadOnlyList<DeliveryCompanyDto>>> GetDeliveryCompanies(
        CancellationToken cancellationToken)
    {
        return Ok(await _deliveryCompanies.GetActiveAsync(cancellationToken));
    }
}

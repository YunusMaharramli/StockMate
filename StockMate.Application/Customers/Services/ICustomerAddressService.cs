using StockMate.Application.Customers.Models;

namespace StockMate.Application.Customers.Services;

public interface ICustomerAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> GetActiveAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> CreateAsync(
        long telegramUserId,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto> UpdateAsync(
        long telegramUserId,
        Guid addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        long telegramUserId,
        Guid addressId,
        CancellationToken cancellationToken = default);
}
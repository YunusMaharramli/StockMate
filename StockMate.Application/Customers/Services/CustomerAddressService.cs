using StockMate.Application.Customers.Models;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;

namespace StockMate.Application.Customers.Services;

public sealed class CustomerAddressService
    : ICustomerAddressService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerAddressRepository
        _customerAddressRepository;

    public CustomerAddressService(
        ICustomerRepository customerRepository,
        ICustomerAddressRepository customerAddressRepository)
    {
        _customerRepository = customerRepository;
        _customerAddressRepository = customerAddressRepository;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> GetActiveAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var addresses =
            await _customerAddressRepository
                .GetActiveByCustomerIdAsync(
                    customer.Id,
                    cancellationToken);

        return addresses
            .Select(MapAddress)
            .ToList();
    }

    public async Task<CustomerAddressDto> CreateAsync(
        long telegramUserId,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var address = new CustomerAddress(
            customer.Id,
            request.AddressLine,
            request.Latitude,
            request.Longitude);

        await _customerAddressRepository.AddAsync(
            address,
            cancellationToken);

        await _customerAddressRepository.SaveChangesAsync(
            cancellationToken);

        return MapAddress(address);
    }

    public async Task<CustomerAddressDto> UpdateAsync(
        long telegramUserId,
        Guid addressId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var address = await GetRequiredOwnedAddressAsync(
            customer.Id,
            addressId,
            cancellationToken);

        address.Update(
            request.AddressLine,
            request.Latitude,
            request.Longitude);

        await _customerAddressRepository.SaveChangesAsync(
            cancellationToken);

        return MapAddress(address);
    }

    public async Task DeactivateAsync(
        long telegramUserId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var address = await GetRequiredOwnedAddressAsync(
            customer.Id,
            addressId,
            cancellationToken);

        address.Deactivate();

        await _customerAddressRepository.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<Customer> GetRequiredCustomerAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        return await _customerRepository
                   .GetByTelegramUserIdAsync(
                       telegramUserId,
                       cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Customer was not found.");
    }

    private async Task<CustomerAddress> GetRequiredOwnedAddressAsync(
        Guid customerId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        var address =
            await _customerAddressRepository.GetByIdAsync(
                addressId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Customer address was not found.");

        if (address.CustomerId != customerId)
        {
            throw new UnauthorizedAccessException(
                "This address does not belong to the customer.");
        }

        return address;
    }

    private static CustomerAddressDto MapAddress(
        CustomerAddress address)
    {
        return new CustomerAddressDto(
            address.Id,
            address.AddressLine,
            address.Latitude,
            address.Longitude,
            address.IsActive);
    }
}
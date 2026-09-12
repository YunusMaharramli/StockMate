using StockMate.Application.Customers.Models;
using StockMate.Application.Persistence;
using StockMate.Application.Notifications;
using StockMate.Domain.Entities;

namespace StockMate.Application.Customers.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUserAccessChangedNotifier _accessNotifier;

    public CustomerService(ICustomerRepository customerRepository, IUserAccessChangedNotifier accessNotifier)
    {
        _customerRepository = customerRepository;
        _accessNotifier = accessNotifier;
    }

    public async Task<CustomerDto> CreateOrUpdateAsync(
        UpsertCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await _customerRepository.GetByTelegramUserIdAsync(
                request.TelegramUserId,
                cancellationToken);

        if (customer is null)
        {
            customer = new Customer(
                request.TelegramUserId,
                request.FirstName,
                request.LastName,
                request.Username);

            await _customerRepository.AddAsync(
                customer,
                cancellationToken);
        }
        else
        {
            customer.UpdateProfile(
                request.FirstName,
                request.LastName,
                request.Username);
        }

        await _customerRepository.SaveChangesAsync(cancellationToken);

        return MapCustomer(customer);
    }

    public async Task<CustomerDto?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await _customerRepository.GetByTelegramUserIdAsync(
                telegramUserId,
                cancellationToken);

        return customer is null
            ? null
            : MapCustomer(customer);
    }

    public async Task UpdateContactInformationAsync(
        long telegramUserId,
        UpdateContactInformationRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        customer.UpdateContactInformation(
            request.PhoneNumber,
            request.DeliveryAddress,
            request.Latitude,
            request.Longitude);

        await _customerRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeProductPageSizeAsync(
        long telegramUserId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        customer.ChangeProductPageSize(pageSize);

        await _customerRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePhoneNumberAsync(
        long telegramUserId,
        string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        customer.ChangePhoneNumber(phoneNumber);
        await _customerRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> GetRequiredCustomerAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        return await _customerRepository.GetByTelegramUserIdAsync(
                   telegramUserId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Customer with Telegram ID '{telegramUserId}' was not found.");
    }

    private static CustomerDto MapCustomer(Customer customer)
    {
        return new CustomerDto(
            customer.Id,
            customer.TelegramUserId,
            customer.FirstName,
            customer.LastName,
            customer.Username,
            customer.PhoneNumber,
            customer.DeliveryAddress,
            customer.Latitude,
            customer.Longitude,
            customer.ProductPageSize,
            customer.IsBlocked,
            customer.BlockedAtUtc);
    }
    public async Task<CustomerDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(
            id,
            cancellationToken);

        return customer is null
            ? null
            : MapCustomer(customer);
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var customers = await _customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(MapCustomer).ToList();
    }

    public async Task BlockAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(
            customerId,
            cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Customer with ID '{customerId}' was not found.");

        customer.Block();
        await _customerRepository.SaveChangesAsync(cancellationToken);
        await _accessNotifier.NotifyAsync(customer.TelegramUserId, cancellationToken);
    }

    public async Task ReactivateAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        customer.Reactivate();
        await _customerRepository.SaveChangesAsync(cancellationToken);
        await _accessNotifier.NotifyAsync(customer.TelegramUserId, cancellationToken);
    }
}

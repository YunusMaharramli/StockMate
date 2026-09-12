using StockMate.Application.Customers.Models;

namespace StockMate.Application.Customers.Services;

public interface ICustomerService
{
    Task<CustomerDto> CreateOrUpdateAsync(
        UpsertCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomerDto?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task UpdateContactInformationAsync(
        long telegramUserId,
        UpdateContactInformationRequest request,
        CancellationToken cancellationToken = default);

    Task UpdatePhoneNumberAsync(
        long telegramUserId,
        string phoneNumber,
        CancellationToken cancellationToken = default);

    Task ChangeProductPageSizeAsync(
        long telegramUserId,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task BlockAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task ReactivateAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}

using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetByIdsAsync(
    IReadOnlyCollection<Guid> customerIds,
    CancellationToken cancellationToken = default);
}

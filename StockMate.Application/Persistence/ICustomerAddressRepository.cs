using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerAddress>> GetActiveByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
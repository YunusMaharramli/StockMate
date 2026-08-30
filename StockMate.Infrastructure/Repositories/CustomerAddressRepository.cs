using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class CustomerAddressRepository
    : ICustomerAddressRepository
{
    private readonly StockMateDbContext _dbContext;

    public CustomerAddressRepository(
        StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CustomerAddress?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CustomerAddresses
            .FirstOrDefaultAsync(
                address => address.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerAddress>>
        GetActiveByCustomerIdAsync(
            Guid customerId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(address =>
                address.CustomerId == customerId &&
                address.IsActive)
            .OrderBy(address => address.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CustomerAddresses.AddAsync(
            address,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
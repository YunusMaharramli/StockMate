using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly StockMateDbContext _dbContext;

    public CustomerRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Customer?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.Id == id,
            cancellationToken);
    }

    public Task<Customer?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.TelegramUserId == telegramUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.FirstName)
            .ThenBy(customer => customer.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Customer customer,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Customers.AddAsync(
            customer,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<Customer>> GetByIdsAsync(
    IReadOnlyCollection<Guid> customerIds,
    CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
            return [];

        var ids = customerIds
            .Distinct()
            .ToArray();

        return await _dbContext.Customers
            .AsNoTracking()
            .Where(customer => ids.Contains(customer.Id))
            .ToListAsync(cancellationToken);
    }
}

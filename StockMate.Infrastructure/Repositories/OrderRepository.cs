using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly StockMateDbContext _dbContext;

    public OrderRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(
                order => order.Id == id,
                cancellationToken);
    }

    public Task<Order?> GetByOrderNumberAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(
                order => order.OrderNumber == orderNumber,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.Status == status)
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(
            order,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusesAsync(
    IReadOnlyCollection<OrderStatus> statuses,
    CancellationToken cancellationToken = default)
    {
        var statusValues = statuses.ToArray();

        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => statusValues.Contains(order.Status))
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
    public async Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
    Guid customerId,
    CancellationToken cancellationToken = default)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
    }
}
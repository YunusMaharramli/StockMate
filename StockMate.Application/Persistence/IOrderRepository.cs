using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByOrderNumberAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByStatusesAsync(
    IReadOnlyCollection<OrderStatus> statuses,
    CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> GetByCustomerIdAsync(
    Guid customerId,
    CancellationToken cancellationToken = default);
}

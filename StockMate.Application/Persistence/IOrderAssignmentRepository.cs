using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface IOrderAssignmentRepository
{
    Task<OrderAssignment?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderAssignment>> GetActiveByStaffIdAsync(Guid staffId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderAssignment assignment, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

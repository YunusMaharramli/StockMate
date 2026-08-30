using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class OrderAssignmentRepository : IOrderAssignmentRepository
{
    private readonly StockMateDbContext _dbContext;
    public OrderAssignmentRepository(StockMateDbContext dbContext) => _dbContext = dbContext;

    public Task<OrderAssignment?> GetActiveByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
        => _dbContext.OrderAssignments.FirstOrDefaultAsync(
            assignment => assignment.OrderId == orderId && assignment.ReleasedAtUtc == null,
            cancellationToken);

    public async Task<IReadOnlyList<OrderAssignment>> GetActiveByStaffIdAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrderAssignments.AsNoTracking()
            .Where(assignment => assignment.StaffId == staffId && assignment.ReleasedAtUtc == null)
            .OrderByDescending(assignment => assignment.AssignedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OrderAssignment assignment, CancellationToken cancellationToken = default)
        => await _dbContext.OrderAssignments.AddAsync(assignment, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}

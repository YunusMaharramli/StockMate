using StockMate.Application.Staff.Models;

namespace StockMate.Application.Staff.Services;

public interface IOrderAssignmentService
{
    Task<OrderAssignmentDto> AssignAsync(
        long orderNumber,
        Guid workerId,
        Guid adminId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderAssignmentDto>> GetWorkerAssignmentsAsync(
        Guid workerId,
        CancellationToken cancellationToken = default);

    Task<OrderAssignmentDto?> GetOrderAssignmentAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        long orderNumber,
        Guid adminId,
        CancellationToken cancellationToken = default);
}

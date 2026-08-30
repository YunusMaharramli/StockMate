using StockMate.Application.Notifications;
using StockMate.Application.Persistence;
using StockMate.Application.Staff.Models;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;
using StaffEntity = StockMate.Domain.Entities.Staff;

namespace StockMate.Application.Staff.Services;

public class OrderAssignmentService : IOrderAssignmentService
{
    private readonly IOrderAssignmentRepository _assignmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IWorkflowNotificationService _notifications;

    public OrderAssignmentService(
        IOrderAssignmentRepository assignmentRepository,
        IOrderRepository orderRepository,
        IStaffRepository staffRepository,
        IShipmentRepository shipmentRepository,
        IWorkflowNotificationService notifications)
    {
        _assignmentRepository = assignmentRepository;
        _orderRepository = orderRepository;
        _staffRepository = staffRepository;
        _shipmentRepository = shipmentRepository;
        _notifications = notifications;
    }

    public async Task<OrderAssignmentDto> AssignAsync(
        long orderNumber,
        Guid workerId,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        var worker = await _staffRepository.GetByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");
        var admin = await _staffRepository.GetByIdAsync(adminId, cancellationToken)
            ?? throw new KeyNotFoundException("Admin was not found.");

        if (!admin.IsActive || admin.Role != StaffRole.Admin)
            throw new UnauthorizedAccessException("Only an active admin can assign orders.");
        if (!worker.IsActive || worker.Role != StaffRole.Worker)
            throw new InvalidOperationException("Order can only be assigned to an active worker.");
        if (order.Status is OrderStatus.PendingAdminApproval or OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException("Order must be confirmed and active.");

        var current = await _assignmentRepository.GetActiveByOrderIdAsync(order.Id, cancellationToken);
        if (current?.StaffId == workerId)
            return Map(current, orderNumber, worker);

        if (current is not null)
        {
            var shipments = await _shipmentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
            if (shipments.Any(shipment =>
                    shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled))
            {
                throw new InvalidOperationException(
                    "Active shipment must be completed or cancelled before changing the worker.");
            }
        }

        current?.Release();
        var assignment = new OrderAssignment(order.Id, worker.Id, admin.Id);
        await _assignmentRepository.AddAsync(assignment, cancellationToken);
        await _assignmentRepository.SaveChangesAsync(cancellationToken);

        var workerName = GetFullName(worker);
        await _notifications.NotifyOrderAssignedAsync(
            orderNumber, worker.TelegramUserId, workerName, cancellationToken);

        return Map(assignment, orderNumber, worker);
    }

    public async Task<IReadOnlyList<OrderAssignmentDto>> GetWorkerAssignmentsAsync(
        Guid workerId,
        CancellationToken cancellationToken = default)
    {
        var worker = await _staffRepository
            .GetByIdAsync(workerId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");
        var assignments = await _assignmentRepository
            .GetActiveByStaffIdAsync(workerId, cancellationToken);
        var result = new List<OrderAssignmentDto>();

        foreach (var assignment in assignments)
        {
            var order = await _orderRepository
                .GetByIdAsync(assignment.OrderId, cancellationToken);
            if (order is not null && order.Status is
                OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.Delivering)
                result.Add(Map(assignment, order.OrderNumber, worker));
        }

        return result;
    }

    public async Task<OrderAssignmentDto?> GetOrderAssignmentAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository
            .GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        var assignment = await _assignmentRepository
            .GetActiveByOrderIdAsync(order.Id, cancellationToken);
        if (assignment is null || order.Status is OrderStatus.Completed or OrderStatus.Cancelled)
            return null;
        var worker = await _staffRepository
            .GetByIdAsync(assignment.StaffId, cancellationToken)
            ?? throw new KeyNotFoundException("Worker was not found.");
        return Map(assignment, orderNumber, worker);
    }

    public async Task ReleaseAsync(
        long orderNumber,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var admin = await _staffRepository
            .GetByIdAsync(adminId, cancellationToken)
            ?? throw new KeyNotFoundException("Admin was not found.");
        if (!admin.IsActive || admin.Role != StaffRole.Admin)
            throw new UnauthorizedAccessException("Only an active admin can release assignments.");

        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        var assignment = await _assignmentRepository.GetActiveByOrderIdAsync(order.Id, cancellationToken);
        if (assignment is null)
            return;

        var shipments = await _shipmentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        if (shipments.Any(shipment =>
                shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Active shipment must be completed or cancelled before releasing the worker.");
        }

        assignment.Release();
        await _assignmentRepository.SaveChangesAsync(cancellationToken);
    }

    private static OrderAssignmentDto Map(OrderAssignment value, long orderNumber, StaffEntity worker)
        => new(value.Id, orderNumber, worker.Id, GetFullName(worker), value.AssignedAtUtc);

    private static string GetFullName(StaffEntity staff)
        => string.Join(" ", new[] { staff.FirstName, staff.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
}

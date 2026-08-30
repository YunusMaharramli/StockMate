using StockMate.Application.Persistence;
using StockMate.Application.Staff.Models;
using StockMate.Domain.Enums;
using StaffEntity = StockMate.Domain.Entities.Staff;

namespace StockMate.Application.Staff.Services;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _staffRepository;
    private readonly IOrderAssignmentRepository _assignmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IShipmentRepository _shipmentRepository;

    public StaffService(
        IStaffRepository staffRepository,
        IOrderAssignmentRepository assignmentRepository,
        IOrderRepository orderRepository,
        IShipmentRepository shipmentRepository)
    {
        _staffRepository = staffRepository;
        _assignmentRepository = assignmentRepository;
        _orderRepository = orderRepository;
        _shipmentRepository = shipmentRepository;
    }

    public async Task<StaffDto> EnsureAdminAsync(
        long telegramUserId,
        string firstName,
        string? lastName,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var existing = await _staffRepository.GetByTelegramUserIdAsync(
            telegramUserId,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Role != StaffRole.Admin)
                throw new InvalidOperationException(
                    "Configured admin Telegram ID belongs to a worker.");

            if (!existing.IsActive)
                existing.Activate();

            await _staffRepository.SaveChangesAsync(cancellationToken);
            return MapStaff(existing);
        }

        var admin = new StaffEntity(
            telegramUserId,
            firstName,
            lastName,
            username,
            StaffRole.Admin);

        await _staffRepository.AddAsync(admin, cancellationToken);
        await _staffRepository.SaveChangesAsync(cancellationToken);
        return MapStaff(admin);
    }

    public async Task<StaffDto> CreateWorkerAsync(
        CreateWorkerRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _staffRepository.GetByTelegramUserIdAsync(
            request.TelegramUserId,
            cancellationToken);

        if (existing is not null)
            throw new InvalidOperationException(
                "A staff member with this Telegram ID already exists.");

        var worker = new StaffEntity(
            request.TelegramUserId,
            request.FirstName,
            request.LastName,
            request.Username,
            StaffRole.Worker);

        await _staffRepository.AddAsync(worker, cancellationToken);
        await _staffRepository.SaveChangesAsync(cancellationToken);
        return MapStaff(worker);
    }

    public async Task<StaffDto?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetByTelegramUserIdAsync(
            telegramUserId,
            cancellationToken);
        return staff is null ? null : MapStaff(staff);
    }

    public async Task<IReadOnlyList<StaffDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var staff = await _staffRepository.GetAllAsync(cancellationToken);
        return staff.Select(MapStaff).ToList();
    }

    public async Task ActivateAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var staff = await GetRequiredStaffAsync(staffId, cancellationToken);
        staff.Activate();
        await _staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var staff = await GetRequiredStaffAsync(staffId, cancellationToken);

        if (staff.Role == StaffRole.Admin)
            throw new InvalidOperationException("Admin cannot be deactivated.");

        var assignments = await _assignmentRepository.GetActiveByStaffIdAsync(
            staff.Id, cancellationToken);
        foreach (var assignment in assignments)
        {
            var order = await _orderRepository.GetByIdAsync(
                assignment.OrderId, cancellationToken);
            if (order?.Status is OrderStatus.Confirmed or
                OrderStatus.Preparing or OrderStatus.Delivering)
            {
                throw new InvalidOperationException(
                    "Worker has an active order assignment.");
            }
        }

        var shipments = await _shipmentRepository.GetByStaffIdAsync(
            staff.Id, cancellationToken);
        if (shipments.Any(shipment =>
                shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Worker has an active shipment.");
        }

        staff.Deactivate();
        await _staffRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<StaffEntity> GetRequiredStaffAsync(
        Guid staffId,
        CancellationToken cancellationToken)
    {
        return await _staffRepository.GetByIdAsync(staffId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Staff member '{staffId}' was not found.");
    }

    private static StaffDto MapStaff(StaffEntity staff)
    {
        return new StaffDto(
            staff.Id,
            staff.TelegramUserId,
            staff.FirstName,
            staff.LastName,
            staff.Username,
            staff.Role,
            staff.IsActive,
            staff.CreatedAtUtc);
    }
}

using StockMate.Application.Common.Outbox;
using StockMate.Application.Persistence;
using StockMate.Application.Sales.Services;
using StockMate.Application.Shipments.Models;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;
using StaffEntity = StockMate.Domain.Entities.Staff;

namespace StockMate.Application.Shipments.Services;

public class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IOrderAssignmentRepository _assignmentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IStaffRepository _staffRepository;
    private readonly IOutboxWriter _outbox;
    private readonly ISaleRecordingService _saleRecordingService;

    public ShipmentService(
        IShipmentRepository shipmentRepository,
        IOrderAssignmentRepository assignmentRepository,
        IOrderRepository orderRepository,
        IStaffRepository staffRepository,
        IOutboxWriter outbox,
        ISaleRecordingService saleRecordingService)
    {
        _shipmentRepository = shipmentRepository;
        _assignmentRepository = assignmentRepository;
        _orderRepository = orderRepository;
        _staffRepository = staffRepository;
        _outbox = outbox;
        _saleRecordingService =
        _saleRecordingService = saleRecordingService;
    }

    public async Task<ShipmentDto> CreateReadyShipmentAsync(
        long orderNumber,
        Guid workerId,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var worker = await GetRequiredStaffAsync(workerId, cancellationToken);
        var admin = await GetRequiredStaffAsync(adminId, cancellationToken);

        if (admin.Role != StaffRole.Admin || !admin.IsActive)
            throw new UnauthorizedAccessException("Только активный администратор может создавать доставки.");
        if (worker.Role != StaffRole.Worker || !worker.IsActive)
            throw new InvalidOperationException("Для доставки нужен активный сотрудник.");
        if (order.Status is not OrderStatus.Confirmed and
            not OrderStatus.Preparing and not OrderStatus.Delivering)
        {
            throw new InvalidOperationException("Для доставки нужен активный заказ.");
        }

        var assignment = await _assignmentRepository.GetActiveByOrderIdAsync(order.Id, cancellationToken);
        if (assignment?.StaffId != workerId)
            throw new InvalidOperationException("Заказ не назначен этому сотруднику.");

        if (order.DeliveryMode != DeliveryMode.PartialAllowed &&
            order.ReadyQuantity < order.RemainingQuantity)
        {
            throw new InvalidOperationException(
                "Заказ должен быть полностью готов, так как частичная доставка запрещена.");
        }

        var existingShipments = await _shipmentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var allocated = new Dictionary<Guid, int>();
        foreach (var existing in existingShipments.Where(value =>
                     value.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled))
        {
            foreach (var item in await _shipmentRepository.GetItemsAsync(existing.Id, cancellationToken))
                allocated[item.OrderItemId] = allocated.GetValueOrDefault(item.OrderItemId) + item.Quantity;
        }

        var quantities = order.Items
            .Select(item => new
            {
                Item = item,
                Quantity = Math.Max(0, item.ReadyQuantity - allocated.GetValueOrDefault(item.Id))
            })
            .Where(value => value.Quantity > 0)
            .ToList();

        if (quantities.Count == 0)
            throw new InvalidOperationException("Нет готовых товаров без назначения.");

        var shipment = new OrderShipment(order.Id, worker.Id);
        await _shipmentRepository.AddAsync(shipment, cancellationToken);
        foreach (var value in quantities)
            await _shipmentRepository.AddItemAsync(
                new OrderShipmentItem(shipment.Id, value.Item.Id, value.Quantity),
                cancellationToken);

        await _shipmentRepository.AddHistoryAsync(
            new ShipmentStatusHistory(shipment.Id, admin.Id, null, ShipmentStatus.Assigned),
            cancellationToken);
        await _shipmentRepository.SaveChangesAsync(cancellationToken);

        return await MapAsync(shipment, order, worker, cancellationToken);
    }

    public async Task<ShipmentDto> ChangeStatusAsync(
        Guid shipmentId,
        Guid actorStaffId,
        ShipmentStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Доставка не найдена.");
        var actor = await GetRequiredStaffAsync(actorStaffId, cancellationToken);
        var worker = await GetRequiredStaffAsync(shipment.StaffId, cancellationToken);

        if (!actor.IsActive ||
            (actor.Role != StaffRole.Admin && actor.Id != shipment.StaffId))
            throw new UnauthorizedAccessException("Сотрудник не может обновить эту доставку.");

        var order = await _orderRepository.GetByIdAsync(shipment.OrderId, cancellationToken)
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var previous = shipment.Status;
        switch (newStatus)
        {
            case ShipmentStatus.Preparing:
                shipment.StartPreparing();
                if (order.Status == OrderStatus.Confirmed)
                    order.MarkAsPreparing();
                break;
            case ShipmentStatus.Ready: shipment.MarkReady(); break;
            case ShipmentStatus.OutForDelivery:
                shipment.MarkOutForDelivery();
                if (order.Status == OrderStatus.Preparing)
                    order.MarkAsDelivering();
                break;
            case ShipmentStatus.Delivered:
                {
                    shipment.MarkDelivered();

                    var shipmentItems =
                        await _shipmentRepository.GetItemsAsync(
                            shipment.Id,
                            cancellationToken);

                    var deliveredQuantities =
                        shipmentItems.ToDictionary(
                            item => item.OrderItemId,
                            item => item.Quantity);

                    order.DeliverShipmentItems(
                        deliveredQuantities);

                    await _saleRecordingService.AddAsync(
                        order,
                        deliveredQuantities,
                        shipment.DeliveredAtUtc!.Value,
                        shipment.Id,
                        cancellationToken);

                    if (order.Status == OrderStatus.Completed)
                    {
                        var assignment =
                            await _assignmentRepository
                                .GetActiveByOrderIdAsync(
                                    order.Id,
                                    cancellationToken);

                        assignment?.Release();
                    }
                }
                break;
            case ShipmentStatus.Cancelled: shipment.Cancel(); break;
            default: throw new InvalidOperationException("Неподдерживаемый статус доставки.");
        }

        await _shipmentRepository.AddHistoryAsync(
            new ShipmentStatusHistory(
                shipment.Id,
                actor.Id,
                previous,
                newStatus),
            cancellationToken);

        await _outbox.EnqueueAsync(
            OutboxMessageTypes.ShipmentStatusChanged,
            new ShipmentStatusChangedOutboxPayload(
                order.OrderNumber,
                shipment.Status,
                GetFullName(worker)),
            cancellationToken);

        await _shipmentRepository.SaveChangesAsync(
            cancellationToken);

        return await MapAsync(shipment, order, worker, cancellationToken);
    }

    public async Task<IReadOnlyList<ShipmentDto>> GetWorkerShipmentsAsync(
        Guid workerId,
        CancellationToken cancellationToken = default)
    {
        var worker = await GetRequiredStaffAsync(workerId, cancellationToken);
        var shipments = await _shipmentRepository.GetByStaffIdAsync(workerId, cancellationToken);
        var result = new List<ShipmentDto>();
        foreach (var shipment in shipments)
        {
            var order = await _orderRepository.GetByIdAsync(shipment.OrderId, cancellationToken);
            if (order is not null)
                result.Add(await MapAsync(shipment, order, worker, cancellationToken));
        }
        return result;
    }

    public async Task<IReadOnlyList<ShipmentDto>> GetOrderShipmentsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var shipments = await _shipmentRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var result = new List<ShipmentDto>();
        foreach (var shipment in shipments)
        {
            var worker = await GetRequiredStaffAsync(shipment.StaffId, cancellationToken);
            result.Add(await MapAsync(shipment, order, worker, cancellationToken));
        }
        return result;
    }

    private async Task<StaffEntity> GetRequiredStaffAsync(Guid id, CancellationToken cancellationToken)
        => await _staffRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Сотрудник не найден.");

    private async Task<ShipmentDto> MapAsync(
        OrderShipment shipment,
        Order order,
        StaffEntity worker,
        CancellationToken cancellationToken)
    {
        var items = await _shipmentRepository.GetItemsAsync(shipment.Id, cancellationToken);
        var orderItems = order.Items.ToDictionary(item => item.Id);
        return new ShipmentDto(
            shipment.Id,
            order.OrderNumber,
            worker.Id,
            GetFullName(worker),
            shipment.Status,
            order.PhoneNumber,
            order.DeliveryAddress,
            order.Latitude,
            order.Longitude,
            items.Select(item => new ShipmentItemDto(
                item.OrderItemId,
                orderItems[item.OrderItemId].ProductName,
                item.Quantity)).ToList(),
            shipment.CreatedAtUtc,
            shipment.DeliveredAtUtc);
    }

    private static string GetFullName(StaffEntity staff)
        => string.Join(" ", new[] { staff.FirstName, staff.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
}

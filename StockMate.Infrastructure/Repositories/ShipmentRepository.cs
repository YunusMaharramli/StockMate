using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class ShipmentRepository : IShipmentRepository
{
    private readonly StockMateDbContext _dbContext;
    public ShipmentRepository(StockMateDbContext dbContext) => _dbContext = dbContext;

    public Task<OrderShipment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.OrderShipments.FirstOrDefaultAsync(shipment => shipment.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OrderShipment>> GetByStaffIdAsync(
        Guid staffId,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrderShipments.AsNoTracking()
            .Where(shipment => shipment.StaffId == staffId)
            .OrderByDescending(shipment => shipment.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderShipment>> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrderShipments.AsNoTracking()
            .Where(shipment => shipment.OrderId == orderId)
            .OrderByDescending(shipment => shipment.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderShipmentItem>> GetItemsAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrderShipmentItems.AsNoTracking()
            .Where(item => item.ShipmentId == shipmentId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ShipmentStatusHistory>> GetHistoryAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
        => await _dbContext.ShipmentStatusHistory.AsNoTracking()
            .Where(history => history.ShipmentId == shipmentId)
            .OrderBy(history => history.ChangedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OrderShipment shipment, CancellationToken cancellationToken = default)
        => await _dbContext.OrderShipments.AddAsync(shipment, cancellationToken);

    public async Task AddItemAsync(OrderShipmentItem item, CancellationToken cancellationToken = default)
        => await _dbContext.OrderShipmentItems.AddAsync(item, cancellationToken);

    public async Task AddHistoryAsync(ShipmentStatusHistory history, CancellationToken cancellationToken = default)
        => await _dbContext.ShipmentStatusHistory.AddAsync(history, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}

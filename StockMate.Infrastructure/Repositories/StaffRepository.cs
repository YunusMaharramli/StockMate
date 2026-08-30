using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Infrastructure.Persistence;
using StaffEntity = StockMate.Domain.Entities.Staff;

namespace StockMate.Infrastructure.Repositories;

public class StaffRepository : IStaffRepository
{
    private readonly StockMateDbContext _dbContext;

    public StaffRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<StaffEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Staff.FirstOrDefaultAsync(
            staff => staff.Id == id,
            cancellationToken);
    }

    public Task<StaffEntity?> GetByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Staff.FirstOrDefaultAsync(
            staff => staff.TelegramUserId == telegramUserId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<StaffEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Staff
            .AsNoTracking()
            .OrderBy(staff => staff.FirstName)
            .ThenBy(staff => staff.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        StaffEntity staff,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Staff.AddAsync(staff, cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}

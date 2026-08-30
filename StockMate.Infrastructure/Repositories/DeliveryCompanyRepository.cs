using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class DeliveryCompanyRepository
    : IDeliveryCompanyRepository
{
    private readonly StockMateDbContext _dbContext;

    public DeliveryCompanyRepository(
        StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DeliveryCompany?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DeliveryCompanies
            .FirstOrDefaultAsync(
                company => company.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryCompany>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeliveryCompanies
            .AsNoTracking()
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeliveryCompany>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DeliveryCompanies
            .AsNoTracking()
            .Where(company => company.IsActive)
            .OrderBy(company => company.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludedCompanyId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLower();

        return _dbContext.DeliveryCompanies.AnyAsync(
            company =>
                company.Name.ToLower() == normalizedName &&
                (!excludedCompanyId.HasValue ||
                 company.Id != excludedCompanyId.Value),
            cancellationToken);
    }

    public async Task AddAsync(
        DeliveryCompany company,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.DeliveryCompanies.AddAsync(
            company,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
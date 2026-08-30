using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface IDeliveryCompanyRepository
{
    Task<DeliveryCompany?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryCompany>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryCompany>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        Guid? excludedCompanyId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        DeliveryCompany company,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
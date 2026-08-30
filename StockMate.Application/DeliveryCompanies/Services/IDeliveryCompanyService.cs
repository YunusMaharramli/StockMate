using StockMate.Application.DeliveryCompanies.Models;

namespace StockMate.Application.DeliveryCompanies.Services;

public interface IDeliveryCompanyService
{
    Task<IReadOnlyList<DeliveryCompanyDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeliveryCompanyDto>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<DeliveryCompanyDto> CreateAsync(
        CreateDeliveryCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<DeliveryCompanyDto> RenameAsync(
        Guid companyId,
        RenameDeliveryCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(
        Guid companyId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
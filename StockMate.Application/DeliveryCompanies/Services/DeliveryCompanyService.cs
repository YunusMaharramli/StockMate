using StockMate.Application.DeliveryCompanies.Models;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;

namespace StockMate.Application.DeliveryCompanies.Services;

public sealed class DeliveryCompanyService
    : IDeliveryCompanyService
{
    private readonly IDeliveryCompanyRepository
        _deliveryCompanyRepository;

    public DeliveryCompanyService(
        IDeliveryCompanyRepository deliveryCompanyRepository)
    {
        _deliveryCompanyRepository = deliveryCompanyRepository;
    }

    public async Task<IReadOnlyList<DeliveryCompanyDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var companies =
            await _deliveryCompanyRepository.GetAllAsync(
                cancellationToken);

        return companies
            .Select(MapCompany)
            .ToList();
    }

    public async Task<IReadOnlyList<DeliveryCompanyDto>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var companies =
            await _deliveryCompanyRepository.GetActiveAsync(
                cancellationToken);

        return companies
            .Select(MapCompany)
            .ToList();
    }

    public async Task<DeliveryCompanyDto> CreateAsync(
        CreateDeliveryCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Delivery company name is required.",
                nameof(request.Name));
        }

        if (await _deliveryCompanyRepository.NameExistsAsync(
                request.Name,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A delivery company with this name already exists.");
        }

        var company = new DeliveryCompany(request.Name);

        await _deliveryCompanyRepository.AddAsync(
            company,
            cancellationToken);

        await _deliveryCompanyRepository.SaveChangesAsync(
            cancellationToken);

        return MapCompany(company);
    }

    public async Task<DeliveryCompanyDto> RenameAsync(
        Guid companyId,
        RenameDeliveryCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException(
                "Delivery company name is required.",
                nameof(request.Name));
        }

        var company = await GetRequiredCompanyAsync(
            companyId,
            cancellationToken);

        if (await _deliveryCompanyRepository.NameExistsAsync(
                request.Name,
                excludedCompanyId: companyId,
                cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException(
                "A delivery company with this name already exists.");
        }

        company.Rename(request.Name);

        await _deliveryCompanyRepository.SaveChangesAsync(
            cancellationToken);

        return MapCompany(company);
    }

    public async Task SetActiveAsync(
        Guid companyId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var company = await GetRequiredCompanyAsync(
            companyId,
            cancellationToken);

        if (isActive)
            company.Activate();
        else
            company.Deactivate();

        await _deliveryCompanyRepository.SaveChangesAsync(
            cancellationToken);
    }

    private async Task<DeliveryCompany> GetRequiredCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        return await _deliveryCompanyRepository.GetByIdAsync(
                   companyId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   "Delivery company was not found.");
    }

    private static DeliveryCompanyDto MapCompany(
        DeliveryCompany company)
    {
        return new DeliveryCompanyDto(
            company.Id,
            company.Name,
            company.IsActive,
            company.CreatedAtUtc,
            company.UpdatedAtUtc);
    }
}

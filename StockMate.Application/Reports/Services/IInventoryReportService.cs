using StockMate.Application.Reports.Models;

namespace StockMate.Application.Reports.Services;

public interface IInventoryReportService
{
    Task<InventoryTurnoverReportDto> GetTurnoverAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? deliveryCompanyId = null,
        CancellationToken cancellationToken = default);
}
using StockMate.Application.Shortages.Models;

namespace StockMate.Application.Shortages.Services;

public interface IShortageService
{
    Task<IReadOnlyList<MissingProductDto>>
        GetMissingProductsAsync(
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissingProductOrderDto>>
        GetMissingProductOrdersAsync(
            Guid productId,
            CancellationToken cancellationToken = default);
}
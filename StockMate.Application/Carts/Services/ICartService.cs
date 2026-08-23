using StockMate.Application.Carts.Models;

namespace StockMate.Application.Carts.Services;

public interface ICartService
{
    Task<CartDto> GetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);

    Task<CartDto> AddOrUpdateItemAsync(
        long telegramUserId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default);

    Task<CartDto> RemoveItemAsync(
        long telegramUserId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task ClearAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default);
}
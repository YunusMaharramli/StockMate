using StockMate.Domain.Entities;

namespace StockMate.Application.Persistence;

public interface ICartRepository
{
    Task<Cart?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default);

    void Remove(Cart cart);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
    Task AddItemAsync(
    CartItem cartItem,
    CancellationToken cancellationToken = default);
}
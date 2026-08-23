using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public class CartRepository : ICartRepository
{
    private readonly StockMateDbContext _dbContext;

    public CartRepository(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Cart?> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Carts
            .Include(cart => cart.Items)
            .FirstOrDefaultAsync(
                cart => cart.CustomerId == customerId,
                cancellationToken);
    }

    public async Task AddAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(
            cart,
            cancellationToken);
    }

    public void Remove(Cart cart)
    {
        _dbContext.Carts.Remove(cart);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task AddItemAsync(
    CartItem cartItem,
    CancellationToken cancellationToken = default)
    {
        await _dbContext.CartItems.AddAsync(
            cartItem,
            cancellationToken);
    }
}
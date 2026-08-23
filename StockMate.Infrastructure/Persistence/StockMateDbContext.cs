using Microsoft.EntityFrameworkCore;
using StockMate.Domain.Entities;

namespace StockMate.Infrastructure.Persistence;

public class StockMateDbContext : DbContext
{
    public StockMateDbContext(
        DbContextOptions<StockMateDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<StockMovement> StockMovements =>
        Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StockMateDbContext).Assembly);
    }
}
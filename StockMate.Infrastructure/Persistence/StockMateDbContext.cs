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
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<OrderAssignment> OrderAssignments => Set<OrderAssignment>();
    public DbSet<OrderShipment> OrderShipments => Set<OrderShipment>();
    public DbSet<OrderShipmentItem> OrderShipmentItems => Set<OrderShipmentItem>();
    public DbSet<ShipmentStatusHistory> ShipmentStatusHistory => Set<ShipmentStatusHistory>();
    public DbSet<CustomerAddress> CustomerAddresses =>
    Set<CustomerAddress>();
    public DbSet<DeliveryCompany> DeliveryCompanies =>
        Set<DeliveryCompany>();
    public DbSet<StockMovement> StockMovements =>
        Set<StockMovement>();
    public DbSet<StockPurchase> StockPurchases =>
    Set<StockPurchase>();
    public DbSet<StockPurchaseItem> StockPurchaseItems =>
        Set<StockPurchaseItem>();
    public DbSet<SaleRecord> SaleRecords =>
    Set<SaleRecord>();
    public DbSet<SaleRecordItem> SaleRecordItems =>
        Set<SaleRecordItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(StockMateDbContext).Assembly);
    }
}

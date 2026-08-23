using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMate.Application.Persistence;
using StockMate.Infrastructure.Persistence;
using StockMate.Infrastructure.Repositories;

namespace StockMate.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Database connection string is required.",
                nameof(connectionString));
        }

        services.AddDbContext<StockMateDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddScoped<ICustomerRepository, CustomerRepository>();

        services.AddScoped<ICartRepository, CartRepository>();

        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
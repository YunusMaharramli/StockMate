using Microsoft.Extensions.DependencyInjection;
using StockMate.Application.Carts.Services;
using StockMate.Application.Customers.Services;
using StockMate.Application.Orders.Services;
using StockMate.Application.Products.Services;

namespace StockMate.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();

        services.AddScoped<ICustomerService, CustomerService>();

        services.AddScoped<ICartService, CartService>();

        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockMate.Application.Persistence;
using StockMate.Application.Shortages.Exports;
using StockMate.Infrastructure.Persistence;
using StockMate.Infrastructure.Repositories;
using StockMate.Infrastructure.Shortages.Exports;

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
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IOrderAssignmentRepository, OrderAssignmentRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<ICustomerAddressRepository,CustomerAddressRepository>();
        services.AddScoped<IDeliveryCompanyRepository,DeliveryCompanyRepository>();
        services.AddScoped<IStockPurchaseRepository,StockPurchaseRepository>();
        services.AddScoped<ISaleRecordRepository,SaleRecordRepository>();
        services.AddScoped<ITelegramInviteRepository,TelegramInviteRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ISubcategoryRepository, SubcategoryRepository>();
        services.AddSingleton<IShortageExcelExporter, ShortageExcelExporter>();
        return services;
    }
}

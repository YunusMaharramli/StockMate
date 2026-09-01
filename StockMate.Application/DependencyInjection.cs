using Microsoft.Extensions.DependencyInjection;
using StockMate.Application.Carts.Services;
using StockMate.Application.Customers.Services;
using StockMate.Application.DeliveryCompanies.Services;
using StockMate.Application.Orders.Services;
using StockMate.Application.Products.Services;
using StockMate.Application.Reports.Services;
using StockMate.Application.Sales.Services;
using StockMate.Application.Shipments.Services;
using StockMate.Application.Shortages.Services;
using StockMate.Application.Staff.Services;
using StockMate.Application.StockPurchases.Services;
using StockMate.Application.TelegramInvites.Services;

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

        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IOrderAssignmentService, OrderAssignmentService>();
        services.AddScoped<ISaleRecordingService, SaleRecordingService>();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IShortageService, ShortageService>();
        services.AddScoped<ICustomerAddressService,CustomerAddressService>();
        services.AddScoped<IDeliveryCompanyService, DeliveryCompanyService>();
        services.AddScoped<IStockPurchaseService,StockPurchaseService>();
        services.AddScoped<IInventoryReportService, InventoryReportService>();
        services.AddScoped<ITelegramInviteService, TelegramInviteService>();
        return services;
    }
}

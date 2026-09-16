using StockMate.Application.Persistence;
using StockMate.Application.Shortages.Models;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Shortages.Services;

public sealed class ShortageService : IShortageService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public ShortageService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IReadOnlyList<MissingProductDto>>
        GetMissingProductsAsync(
            CancellationToken cancellationToken = default)
    {
        var orders =
            await _orderRepository.GetOrdersWithShortagesAsync(
                cancellationToken);

        return orders
            .SelectMany(order => order.Items)
            .Where(IsMissing)
            .GroupBy(item => new
            {
                item.ProductId,
                item.ProductName,
                item.ProductCode
            })
            .Select(group => new MissingProductDto(
                group.Key.ProductId,
                group.Key.ProductName,
                group.Key.ProductCode,
                group.Sum(item => item.SpecialOrderQuantity),
                group.Select(item => item.OrderId)
                    .Distinct()
                    .Count()))
            .OrderBy(item => item.ProductName)
            .ToList();
    }

    public async Task<IReadOnlyList<MissingProductOrderDto>>
        GetMissingProductOrdersAsync(
            Guid productId,
            CancellationToken cancellationToken = default)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException(
                "ID товара обязателен.",
                nameof(productId));
        }

        var orders =
            await _orderRepository.GetOrdersWithShortagesAsync(
                cancellationToken);

        var affectedOrders = orders
            .Where(order => order.Items.Any(item =>
                item.ProductId == productId &&
                IsMissing(item)))
            .ToList();

        var customerIds = affectedOrders
            .Select(order => order.CustomerId)
            .Distinct()
            .ToArray();

        var customers =
            await _customerRepository.GetByIdsAsync(
                customerIds,
                cancellationToken);

        var customersById = customers.ToDictionary(
            customer => customer.Id);

        return affectedOrders
            .Select(order =>
            {
                var item = order.Items.First(item =>
                    item.ProductId == productId &&
                    IsMissing(item));

                customersById.TryGetValue(
                    order.CustomerId,
                    out var customer);

                return new MissingProductOrderDto(
                    order.OrderNumber,
                    order.CustomerId,
                    GetCustomerName(customer),
                    item.SpecialOrderQuantity,
                    item.SpecialOrderStatus);
            })
            .OrderBy(item => item.OrderNumber)
            .ToList();
    }

    private static bool IsMissing(OrderItem item)
    {
        return item.SpecialOrderQuantity > 0 &&
               item.SpecialOrderStatus is
                   SpecialOrderStatus.Pending or
                   SpecialOrderStatus.OrderedFromSupplier;
    }

    private static string GetCustomerName(Customer? customer)
    {
        if (customer is null)
            return "Клиент не найден";

        return string.Join(
            " ",
            new[]
            {
                customer.FirstName,
                customer.LastName
            }.Where(value =>
                !string.IsNullOrWhiteSpace(value)));
    }
}
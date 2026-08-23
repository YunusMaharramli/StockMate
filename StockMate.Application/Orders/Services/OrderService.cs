using StockMate.Application.Orders.Models;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public async Task<OrderDto> CheckoutAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await _customerRepository.GetByTelegramUserIdAsync(
                telegramUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Customer with Telegram ID '{telegramUserId}' was not found.");

        if (string.IsNullOrWhiteSpace(customer.PhoneNumber) ||
            string.IsNullOrWhiteSpace(customer.DeliveryAddress))
        {
            throw new InvalidOperationException(
                "Phone number and delivery address are required.");
        }

        var cart = await _cartRepository.GetByCustomerIdAsync(
                       customer.Id,
                       cancellationToken)
                   ?? throw new InvalidOperationException(
                       "Customer cart was not found.");

        if (cart.IsExpired)
            throw new InvalidOperationException("Cart has expired.");

        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Cart is empty.");

        var productIds = cart.Items
            .Select(item => item.ProductId)
            .ToArray();

        var products = await _productRepository.GetByIdsAsync(
            productIds,
            cancellationToken);

        var productsById = products.ToDictionary(
            product => product.Id);

        var order = new Order(
            customer.Id,
            customer.PhoneNumber,
            customer.DeliveryAddress,
            customer.Latitude,
            customer.Longitude);

        foreach (var cartItem in cart.Items)
        {
            if (!productsById.TryGetValue(
                    cartItem.ProductId,
                    out var product))
            {
                throw new KeyNotFoundException(
                    $"Product '{cartItem.ProductId}' was not found.");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException(
                    $"Product '{product.Name}' is inactive.");
            }

            var fromStockQuantity = Math.Min(
                cartItem.Quantity,
                product.StockQuantity);

            var missingQuantity =
                cartItem.Quantity - fromStockQuantity;

            var specialOrderQuantity = missingQuantity;

            order.AddItem(
                product,
                fromStockQuantity,
                specialOrderQuantity);
        }

        await _orderRepository.AddAsync(
            order,
            cancellationToken);

        cart.Clear();

        await _orderRepository.SaveChangesAsync(cancellationToken);

        return MapOrder(order);
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(
            orderNumber,
            cancellationToken);

        return order is null ? null : MapOrder(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusAsync(
            status,
            cancellationToken);

        return orders.Select(MapOrder).ToList();
    }

    public async Task ConfirmAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        var productIds = order.Items
            .Select(item => item.ProductId)
            .ToArray();

        var products = await _productRepository.GetByIdsAsync(
            productIds,
            cancellationToken);

        var productsById = products.ToDictionary(
            product => product.Id);

        foreach (var item in order.Items)
        {
            if (!productsById.TryGetValue(
                    item.ProductId,
                    out var product))
            {
                throw new KeyNotFoundException(
                    $"Product '{item.ProductId}' was not found.");
            }

            if (item.FromStockQuantity == 0)
                continue;

            var movement = product.DeductForOrder(
                item.FromStockQuantity,
                order.Id);

            _productRepository.Update(product);

            await _productRepository.AddStockMovementAsync(
                movement,
                cancellationToken);
        }

        order.Confirm();

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsPreparingAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        order.MarkAsPreparing();

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsDeliveringAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        if (order.Items.Any(item =>
                item.SpecialOrderQuantity > 0 &&
                item.SpecialOrderStatus !=
                SpecialOrderStatus.Arrived &&
                item.SpecialOrderStatus !=
                SpecialOrderStatus.Completed))
        {
            throw new InvalidOperationException(
                "Special-order products have not arrived yet.");
        }

        order.MarkAsDelivering();

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        if (order.RemainingQuantity > 0)
            order.DeliverReadyItems();
        else if (order.Status != OrderStatus.Completed)
            order.Complete();

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderDto> DeliverReadyItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(orderNumber, cancellationToken);

        order.DeliverReadyItems();

        await _orderRepository.SaveChangesAsync(cancellationToken);

        return MapOrder(order);
    }

    public async Task<OrderDto> CancelRemainingItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(orderNumber, cancellationToken);

        await RestoreUndeliveredStockAsync(order, cancellationToken);
        order.CancelRemainingItems();

        await _orderRepository.SaveChangesAsync(cancellationToken);

        return MapOrder(order);
    }

    public async Task CancelAsync(
        long orderNumber,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        var stockWasDeducted =
            order.Status is OrderStatus.Confirmed
                or OrderStatus.Preparing
                or OrderStatus.Delivering;

        if (stockWasDeducted)
        {
            await RestoreUndeliveredStockAsync(order, cancellationToken);
        }

        order.Cancel(reason);

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDeliveryFeeAsync(
        long orderNumber,
        decimal deliveryFee,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        order.SetDeliveryFee(deliveryFee);

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetAdminNoteAsync(
        long orderNumber,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        order.SetAdminNote(adminNote);

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDeliveryModeAsync(
        long orderNumber,
        DeliveryMode deliveryMode,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(orderNumber, cancellationToken);

        order.SetDeliveryMode(deliveryMode);

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeSpecialOrderStatusAsync(
        long orderNumber,
        Guid orderItemId,
        SpecialOrderStatus status,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(
            orderNumber,
            cancellationToken);

        var item = order.Items.FirstOrDefault(
                       item => item.Id == orderItemId)
                   ?? throw new KeyNotFoundException(
                       $"Order item '{orderItemId}' was not found.");

        switch (status)
        {
            case SpecialOrderStatus.OrderedFromSupplier:
                item.MarkAsOrderedFromSupplier();
                break;

            case SpecialOrderStatus.Arrived:
                item.MarkSpecialOrderAsArrived();
                break;

            case SpecialOrderStatus.Completed:
                item.CompleteSpecialOrder();
                break;

            case SpecialOrderStatus.Cancelled:
                item.CancelSpecialOrder();
                break;

            default:
                throw new InvalidOperationException(
                    $"Special-order status '{status}' cannot be assigned manually.");
        }

        await _orderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Order> GetRequiredOrderAsync(
        long orderNumber,
        CancellationToken cancellationToken)
    {
        return await _orderRepository.GetByOrderNumberAsync(
                   orderNumber,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Order number '{orderNumber}' was not found.");
    }

    private async Task RestoreUndeliveredStockAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var quantities = order.Items
            .Select(item => new
            {
                Item = item,
                Quantity = Math.Min(
                    item.RemainingQuantity,
                    Math.Max(0, item.FromStockQuantity - item.DeliveredQuantity))
            })
            .Where(entry => entry.Quantity > 0)
            .ToList();

        if (quantities.Count == 0)
            return;

        var products = await _productRepository.GetByIdsAsync(
            quantities.Select(entry => entry.Item.ProductId).ToArray(),
            cancellationToken);

        var productsById = products.ToDictionary(product => product.Id);

        foreach (var entry in quantities)
        {
            var product = productsById[entry.Item.ProductId];
            var movement = product.RestoreFromCancelledOrder(
                entry.Quantity,
                order.Id);

            _productRepository.Update(product);
            await _productRepository.AddStockMovementAsync(
                movement,
                cancellationToken);
        }
    }

    private static OrderDto MapOrder(Order order)
    {
        var items = order.Items
            .Select(item => new OrderItemDto(
                item.Id,
                item.ProductId,
                item.ProductName,
                item.ProductCode,
                item.UnitPrice,
                item.FromStockQuantity,
                item.SpecialOrderQuantity,
                item.DeliveredQuantity,
                item.CancelledQuantity,
                item.RemainingQuantity,
                item.ReadyQuantity,
                item.TotalQuantity,
                item.LineTotal,
                item.SpecialOrderStatus))
            .ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.PhoneNumber,
            order.DeliveryAddress,
            order.Latitude,
            order.Longitude,
            order.Status,
            order.DeliveryMode,
            items,
            order.ProductsTotal,
            order.DeliveryFee,
            order.TotalAmount,
            order.ContainsSpecialOrder,
            order.FulfillmentStatus,
            order.RemainingQuantity,
            order.ReadyQuantity,
            order.AdminNote,
            order.CancellationReason,
            order.CreatedAtUtc,
            order.ConfirmedAtUtc,
            order.CompletedAtUtc,
            order.CancelledAtUtc);
    }
    public async Task<IReadOnlyList<OrderDto>> GetNewOrdersAsync(
    CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusesAsync(
            [OrderStatus.PendingAdminApproval],
            cancellationToken);

        return orders.Select(MapOrder).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetActiveOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusesAsync(
            [
                OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.Delivering
            ],
            cancellationToken);

        return orders.Select(MapOrder).ToList();
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrderHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByStatusesAsync(
            [
                OrderStatus.Completed,
            OrderStatus.Cancelled
            ],
            cancellationToken);

        return orders.Select(MapOrder).ToList();
    }
    public async Task<IReadOnlyList<OrderDto>> GetCustomerOrdersAsync(
    long telegramUserId,
    CancellationToken cancellationToken = default)
    {
        var customer =
            await _customerRepository.GetByTelegramUserIdAsync(
                telegramUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var orders =
            await _orderRepository.GetByCustomerIdAsync(
                customer.Id,
                cancellationToken);

        return orders.Select(MapOrder).ToList();
    }
}

using StockMate.Application.Orders.Models;
using StockMate.Application.Persistence;
using StockMate.Application.Sales.Services;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Orders.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IShipmentRepository _shipmentRepository;
    private readonly ICustomerAddressRepository _customerAddressRepository;
    private readonly IDeliveryCompanyRepository _deliveryCompanyRepository;
    private readonly ISaleRecordingService _saleRecordingService;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IShipmentRepository shipmentRepository,
        ICustomerAddressRepository customerAddressRepository,
        IDeliveryCompanyRepository deliveryCompanyRepository,
        ISaleRecordingService saleRecordingService)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _shipmentRepository = shipmentRepository;
        _customerAddressRepository = customerAddressRepository;
        _deliveryCompanyRepository = deliveryCompanyRepository;
        _saleRecordingService = saleRecordingService;
    }

    public async Task<OrderDto> CheckoutAsync(
        long telegramUserId,
        Guid customerAddressId,
        Guid deliveryCompanyId,
        CancellationToken cancellationToken = default)
    {
        if (customerAddressId == Guid.Empty)
        {
            throw new ArgumentException(
                "Customer address ID is required.",
                nameof(customerAddressId));
        }

        if (deliveryCompanyId == Guid.Empty)
        {
            throw new ArgumentException(
                "Delivery company ID is required.",
                nameof(deliveryCompanyId));
        }

        var customer =
            await _customerRepository.GetByTelegramUserIdAsync(
                telegramUserId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Customer with Telegram ID '{telegramUserId}' was not found.");

        if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            throw new InvalidOperationException(
                "Phone number is required.");
        }

        var address =
            await _customerAddressRepository.GetByIdAsync(
                customerAddressId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Customer address was not found.");

        if (address.CustomerId != customer.Id)
        {
            throw new UnauthorizedAccessException(
                "The selected address does not belong to this customer.");
        }

        if (!address.IsActive)
        {
            throw new InvalidOperationException(
                "The selected address is inactive.");
        }

        var deliveryCompany =
            await _deliveryCompanyRepository.GetByIdAsync(
                deliveryCompanyId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                "Delivery company was not found.");

        if (!deliveryCompany.IsActive)
        {
            throw new InvalidOperationException(
                "The selected delivery company is inactive.");
        }

        return await CreateOrderAsync(
            customer,
            address.AddressLine,
            address.Latitude,
            address.Longitude,
            address,
            deliveryCompany,
            cancellationToken);
    }

    private async Task<OrderDto> CreateOrderAsync(
        Customer customer,
        string deliveryAddress,
        double? latitude,
        double? longitude,
        CustomerAddress selectedAddress,
        DeliveryCompany deliveryCompany,
        CancellationToken cancellationToken)
    {
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

        var pendingOrders =
            await _orderRepository.GetByStatusAsync(
                OrderStatus.PendingAdminApproval,
                cancellationToken);

        var reservedStockByProduct = pendingOrders
            .SelectMany(existingOrder => existingOrder.Items)
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item =>
                    item.FromStockQuantity));

        var order = new Order(
            customer.Id,
            customer.PhoneNumber!,
            deliveryAddress,
            latitude,
            longitude);

        order.SelectDeliveryDetails(
            selectedAddress,
            deliveryCompany);

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

            var reservedQuantity =
                reservedStockByProduct.GetValueOrDefault(
                    product.Id);

            var availableStockQuantity = Math.Max(
                0,
                product.StockQuantity - reservedQuantity);

            var fromStockQuantity = Math.Min(
                cartItem.Quantity,
                availableStockQuantity);

            var missingQuantity =
                cartItem.Quantity - fromStockQuantity;

            order.AddItem(
                product,
                fromStockQuantity,
                missingQuantity);
        }

        await _orderRepository.AddAsync(
            order,
            cancellationToken);

        cart.Clear();

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

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

        await EnsureNoActiveShipmentsAsync(order, cancellationToken);

        if (order.RemainingQuantity > 0)
        {
            var deliveredQuantities =
                order.DeliverReadyItems();

            await _saleRecordingService.AddAsync(
                order,
                deliveredQuantities,
                DateTime.UtcNow,
                shipmentId: null,
                cancellationToken);
        }
        else if (order.Status != OrderStatus.Completed)
        {
            order.Complete();
        }

        await _orderRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<OrderDto> DeliverReadyItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(orderNumber, cancellationToken);

        await EnsureNoActiveShipmentsAsync(order, cancellationToken);

        var deliveredQuantities =
    order.DeliverReadyItems();

        await _saleRecordingService.AddAsync(
            order,
            deliveredQuantities,
            DateTime.UtcNow,
            shipmentId: null,
            cancellationToken);

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        return MapOrder(order);
    }

    public async Task<OrderDto> CancelRemainingItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await GetRequiredOrderAsync(orderNumber, cancellationToken);

        await EnsureNoActiveShipmentsAsync(order, cancellationToken);

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

        await EnsureNoActiveShipmentsAsync(order, cancellationToken);

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

    private async Task EnsureNoActiveShipmentsAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var shipments = await _shipmentRepository.GetByOrderIdAsync(
            order.Id, cancellationToken);

        if (shipments.Any(shipment =>
                shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled))
        {
            throw new InvalidOperationException(
                "Active shipment must be completed or cancelled first.");
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
            order.CustomerAddressId,
            order.PhoneNumber,
            order.DeliveryAddress,
            order.Latitude,
            order.Longitude,
            order.DeliveryCompanyId,
            order.DeliveryCompanyName,
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

    public async Task<IReadOnlyList<OrderDto>> GetCustomerOrdersByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(
            customerId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Customer was not found.");

        var orders = await _orderRepository.GetByCustomerIdAsync(
            customer.Id,
            cancellationToken);

        return orders.Select(MapOrder).ToList();
    }
}

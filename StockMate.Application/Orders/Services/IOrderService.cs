using StockMate.Application.Orders.Models;
using StockMate.Domain.Enums;

namespace StockMate.Application.Orders.Services;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(
        long telegramUserId,
        Guid customerAddressId,
        Guid deliveryCompanyId,
        CancellationToken cancellationToken = default);

    Task<OrderDto?> GetByOrderNumberAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken = default);

    Task ConfirmAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task MarkAsPreparingAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task MarkAsDeliveringAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task<OrderDto> DeliverReadyItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CancelRemainingItemsAsync(
        long orderNumber,
        CancellationToken cancellationToken = default);

    Task CancelAsync(
        long orderNumber,
        string reason,
        CancellationToken cancellationToken = default);

    Task SetDeliveryFeeAsync(
        long orderNumber,
        decimal deliveryFee,
        CancellationToken cancellationToken = default);

    Task SetAdminNoteAsync(
        long orderNumber,
        string? adminNote,
        CancellationToken cancellationToken = default);

    Task SetDeliveryModeAsync(
        long orderNumber,
        DeliveryMode deliveryMode,
        CancellationToken cancellationToken = default);

    Task ChangeSpecialOrderStatusAsync(
        long orderNumber,
        Guid orderItemId,
        SpecialOrderStatus status,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetNewOrdersAsync(
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetActiveOrdersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetOrderHistoryAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetCustomerOrdersAsync(
    long telegramUserId,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetCustomerOrdersByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Options;
using StockMate.Application.Customers.Services;
using StockMate.Application.Notifications;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Options;
using StockMate.Domain.Enums;
using Telegram.Bot;

namespace StockMate.Bot.Telegram.Services;

public class WorkflowNotificationService : IWorkflowNotificationService
{
    private readonly TelegramBotClient _botClient;
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly TelegramOptions _options;
    private readonly ILogger<WorkflowNotificationService> _logger;

    public WorkflowNotificationService(
        TelegramBotClient botClient,
        IOrderService orderService,
        ICustomerService customerService,
        IOptions<TelegramOptions> options,
        ILogger<WorkflowNotificationService> logger)
    {
        _botClient = botClient;
        _orderService = orderService;
        _customerService = customerService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyOrderConfirmedAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        var customer = await _customerService.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer was not found.");

        await SafeSendAsync(
            customer.TelegramUserId,
            $"✅ Sifarişiniz təsdiqləndi.\n\n" +
            $"Sifariş nömrəsi: #{orderNumber}\n" +
            "Sifarişiniz hazırlanma mərhələsinə keçəcək.",
            cancellationToken);

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"✅ Sifariş #{orderNumber} təsdiqləndi.\n" +
                $"Müştəri: {GetCustomerName(customer.FirstName, customer.LastName)}",
                cancellationToken);
        }
    }

    public async Task NotifyOrderAssignedAsync(
        long orderNumber,
        long workerTelegramUserId,
        string workerName,
        CancellationToken cancellationToken = default)
    {
        await SafeSendAsync(
            workerTelegramUserId,
            $"📋 Sizə sifariş #{orderNumber} təyin edildi.",
            cancellationToken);

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"👷 Sifariş #{orderNumber} işçiyə təyin edildi.\nİşçi: {workerName}",
                cancellationToken);
        }
    }

    public async Task NotifyShipmentStatusChangedAsync(
        long orderNumber,
        ShipmentStatus status,
        string workerName,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Order was not found.");
        var customer = await _customerService.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Customer was not found.");
        var customerName = GetCustomerName(customer.FirstName, customer.LastName);

        var adminText = status switch
        {
            ShipmentStatus.Preparing => "hazırlamağa başladı",
            ShipmentStatus.Ready => "hazır olduğunu bildirdi",
            ShipmentStatus.OutForDelivery => "çatdırılmağa götürdü",
            ShipmentStatus.Delivered => "çatdırıldığını bildirdi",
            ShipmentStatus.Cancelled => "çatdırılmanı ləğv etdi",
            _ => "statusu dəyişdi"
        };

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"📦 Sifariş #{orderNumber} — Müştəri: {customerName}\n" +
                $"👷 {workerName} {adminText}.",
                cancellationToken);
        }

        var customerText = status switch
        {
            ShipmentStatus.Ready => "✅ Sifarişiniz hazırdır. Tezliklə çatdırılmağa veriləcək.",
            ShipmentStatus.OutForDelivery => "🚚 Sifarişiniz çatdırılmağa verildi.",
            ShipmentStatus.Delivered when order.RemainingQuantity > 0 =>
                $"✅ Sifarişinizin bir hissəsi çatdırıldı. Qalan məhsul sayı: {order.RemainingQuantity}.",
            ShipmentStatus.Delivered => "✅ Sifarişiniz tam çatdırıldı.",
            _ => null
        };

        if (customerText is not null)
        {
            await SafeSendAsync(
                customer.TelegramUserId,
                $"{customerText}\n\nSifariş: #{orderNumber}",
                cancellationToken);
        }
    }

    private static string GetCustomerName(string firstName, string? lastName)
        => string.Join(
            " ",
            new[] { firstName, lastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

    private async Task SafeSendAsync(
        long telegramUserId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SendMessage(
                telegramUserId,
                text,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Telegram notification could not be sent to user {TelegramUserId}.",
                telegramUserId);
        }
    }
}

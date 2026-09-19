using Microsoft.Extensions.Options;
using StockMate.Application.Customers.Services;
using StockMate.Application.Notifications;
using StockMate.Application.Orders.Services;
using StockMate.Bot.Options;
using StockMate.Domain.Enums;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

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

    public async Task NotifyOrderCreatedAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        if (_options.AdminUserId <= 0)
            return;

        var order = await _orderService.GetByOrderNumberAsync(
            orderNumber,
            cancellationToken)
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var customer = await _customerService.GetByIdAsync(
            order.CustomerId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Клиент не найден.");
        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
            _options.AdminUserId,
            $"🆕 Новый заказ #{orderNumber}\n" +
            $"Клиент: {GetCustomerName(customer.FirstName, customer.LastName)}\n" +
            $"Сумма: {order.ProductsTotal:0.00} AZN",
            cancellationToken,
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "✅ Подтвердить",
                            $"admin:confirm:{orderNumber}"),

                        InlineKeyboardButton.WithCallbackData(
                            "❌ Отменить",
                            $"admin:cancel:{orderNumber}")
                    }
                }));
        await SafeSendAsync(
            customer.TelegramUserId,
            $"✅ Ваш заказ принят.\n\n" +
            $"Номер заказа: #{order.OrderNumber}\n" +
            $"Товары: {order.ProductsTotal:0.00} AZN\n" +
            "Стоимость доставки будет согласована " +
            "с администратором.",
            cancellationToken);
        }
        
    }

    public async Task NotifyOrderConfirmedAsync(
        long orderNumber,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderService.GetByOrderNumberAsync(orderNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var customer = await _customerService.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Клиент не найден.");

        await SafeSendAsync(
            customer.TelegramUserId,
            $"✅ Ваш заказ подтверждён.\n\n" +
            $"Номер заказа: #{orderNumber}\n" +
            "Ваш заказ перейдёт на этап подготовки.",
            cancellationToken);

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"✅ Заказ №{orderNumber} подтверждён.\n" +
                $"Клиент: {GetCustomerName(customer.FirstName, customer.LastName)}",
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
            $"📋 Вам назначен заказ №{orderNumber}.",
            cancellationToken);

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"👷 Заказ №{orderNumber} назначен сотруднику.\nСотрудник: {workerName}",
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
            ?? throw new KeyNotFoundException("Заказ не найден.");
        var customer = await _customerService.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new KeyNotFoundException("Клиент не найден.");
        var customerName = GetCustomerName(customer.FirstName, customer.LastName);

        var adminText = status switch
        {
            ShipmentStatus.Preparing => "начал подготовку",
            ShipmentStatus.Ready => "сообщил о готовности",
            ShipmentStatus.OutForDelivery => "принял в доставку",
            ShipmentStatus.Delivered => "сообщил о доставке",
            ShipmentStatus.Cancelled => "отменил доставку",
            _ => "изменил статус"
        };

        if (_options.AdminUserId > 0)
        {
            await SafeSendAsync(
                _options.AdminUserId,
                $"📦 Заказ №{orderNumber} — Клиент: {customerName}\n" +
                $"👷 {workerName} {adminText}.",
                cancellationToken);
        }

        var customerText = status switch
        {
            ShipmentStatus.Ready => "✅ Ваш заказ готов. Скоро он будет передан в доставку.",
            ShipmentStatus.OutForDelivery => "🚚 Ваш заказ передан в доставку.",
            ShipmentStatus.Delivered when order.RemainingQuantity > 0 =>
                $"✅ Часть вашего заказа доставлена. Осталось товаров: {order.RemainingQuantity}.",
            ShipmentStatus.Delivered => "✅ Ваш заказ доставлен полностью.",
            _ => null
        };

        if (customerText is not null)
        {
            await SafeSendAsync(
                customer.TelegramUserId,
                $"{customerText}\n\nЗаказ: №{orderNumber}",
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
    CancellationToken cancellationToken,
    InlineKeyboardMarkup? replyMarkup = null)
    {
        try
        {
            await _botClient.SendMessage(
                telegramUserId,
                text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Telegram notification could not be sent to user " +
                "{TelegramUserId}.",
                telegramUserId);

            throw;
        }
    }
}

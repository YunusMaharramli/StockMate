using System.Text.Json;
using StockMate.Application.Common.Outbox;
using StockMate.Application.Notifications;
using Telegram.Bot;

namespace StockMate.Bot.Outbox;

public sealed class OutboxMessageDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IWorkflowNotificationService _notifications;
    private readonly TelegramBotClient _botClient;

    public OutboxMessageDispatcher(
        IWorkflowNotificationService notifications,
        TelegramBotClient botClient)
    {
        _notifications = notifications;
        _botClient = botClient;
    }

    public async Task DispatchAsync(
        OutboxMessageData message,
        CancellationToken cancellationToken)
    {
        switch (message.MessageType)
        {
            case OutboxMessageTypes.OrderCreated:
                {
                    var payload =
                        Deserialize<OrderCreatedOutboxPayload>(
                            message.PayloadJson);

                    await _notifications.NotifyOrderCreatedAsync(
                        payload.OrderNumber,
                        cancellationToken);

                    break;
                }

            case OutboxMessageTypes.OrderConfirmed:
                {
                    var payload =
                        Deserialize<OrderConfirmedOutboxPayload>(
                            message.PayloadJson);

                    await _notifications.NotifyOrderConfirmedAsync(
                        payload.OrderNumber,
                        cancellationToken);

                    break;
                }

            case OutboxMessageTypes.OrderAssigned:
                {
                    var payload =
                        Deserialize<OrderAssignedOutboxPayload>(
                            message.PayloadJson);

                    await _notifications.NotifyOrderAssignedAsync(
                        payload.OrderNumber,
                        payload.WorkerTelegramUserId,
                        payload.WorkerName,
                        cancellationToken);

                    break;
                }

            case OutboxMessageTypes.ShipmentStatusChanged:
                {
                    var payload =
                        Deserialize<ShipmentStatusChangedOutboxPayload>(
                            message.PayloadJson);

                    await _notifications.NotifyShipmentStatusChangedAsync(
                        payload.OrderNumber,
                        payload.Status,
                        payload.WorkerName,
                        cancellationToken);

                    break;
                }

            case OutboxMessageTypes.TelegramMessage:
                {
                    var payload =
                        Deserialize<TelegramMessageOutboxPayload>(
                            message.PayloadJson);

                    await _botClient.SendMessage(
                        payload.ChatId,
                        payload.Text,
                        cancellationToken: cancellationToken);

                    break;
                }

            default:
                throw new NotSupportedException(
                    $"Unsupported Outbox message type: " +
                    $"'{message.MessageType}'.");
        }
    }

    private static TPayload Deserialize<TPayload>(
        string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<TPayload>(
            payloadJson,
            JsonOptions);

        return payload
            ?? throw new JsonException(
                $"Outbox payload could not be deserialized " +
                $"as {typeof(TPayload).Name}.");
    }
}

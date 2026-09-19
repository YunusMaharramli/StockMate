using StockMate.Domain.Enums;

namespace StockMate.Application.Common.Outbox;

public sealed record OrderCreatedOutboxPayload(
    long OrderNumber);

public sealed record OrderConfirmedOutboxPayload(
    long OrderNumber);

public sealed record OrderAssignedOutboxPayload(
    long OrderNumber,
    long WorkerTelegramUserId,
    string WorkerName);

public sealed record ShipmentStatusChangedOutboxPayload(
    long OrderNumber,
    ShipmentStatus Status,
    string WorkerName);

public sealed record TelegramMessageOutboxPayload(
    long ChatId,
    string Text);

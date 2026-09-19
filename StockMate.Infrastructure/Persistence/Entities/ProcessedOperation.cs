namespace StockMate.Infrastructure.Persistence.Entities;

public sealed class ProcessedOperation
{
    private ProcessedOperation()
    {
    }

    public ProcessedOperation(
    Guid operationId,
    long telegramUserId,
    string operationType,
    string payloadHash)
    {
        if (operationId == Guid.Empty)
            throw new ArgumentException(
                "Operation ID is required.",
                nameof(operationId));

        if (telegramUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(telegramUserId));

        if (string.IsNullOrWhiteSpace(operationType))
            throw new ArgumentException(
                "Operation type is required.",
                nameof(operationType));

        if (string.IsNullOrWhiteSpace(payloadHash))
            throw new ArgumentException(
                "Payload hash is required.",
                nameof(payloadHash));

        Id = Guid.NewGuid();
        OperationId = operationId;
        TelegramUserId = telegramUserId;
        OperationType = operationType.Trim();
        PayloadHash = payloadHash;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid OperationId { get; private set; }
    public long TelegramUserId { get; private set; }

    public string OperationType { get; private set; } = string.Empty;

    public string PayloadHash { get; private set; } = string.Empty;

    public string? ResultJson { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public bool IsCompleted { get; private set; }

    public void Complete(string? resultJson)
    {
        if (IsCompleted)
            throw new InvalidOperationException(
                "The operation has already been completed.");

        ResultJson = resultJson;
        IsCompleted = true;
        CompletedAtUtc = DateTime.UtcNow;
    }
}
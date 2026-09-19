namespace StockMate.Application.Common.Idempotency;

public sealed class IdempotencyConflictException
    : InvalidOperationException
{
    public IdempotencyConflictException(Guid operationId)
        : base(
            "Операция с этим идентификатором уже была " +
            "отправлена с другими данными.")
    {
        OperationId = operationId;
    }

    public Guid OperationId { get; }
}
namespace StockMate.Bot.Extensions;

public static class IdempotencyRequestExtensions
{
    public const string HeaderName = "Idempotency-Key";

    public static Guid GetRequiredOperationId(
        this HttpRequest request)
    {
        var value = request.Headers[HeaderName].ToString();

        if (!Guid.TryParse(value, out var operationId) ||
            operationId == Guid.Empty)
        {
            throw new ArgumentException(
                $"A valid '{HeaderName}' header is required.");
        }

        return operationId;
    }
}
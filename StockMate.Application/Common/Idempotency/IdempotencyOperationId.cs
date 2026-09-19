using System.Security.Cryptography;
using System.Text;

namespace StockMate.Application.Common.Idempotency;

public static class IdempotencyOperationId
{
    public static Guid FromSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException(
                "Operation source is required.",
                nameof(source));

        var sourceBytes = Encoding.UTF8.GetBytes(
            source.Trim());

        var hash = SHA256.HashData(sourceBytes);

        return new Guid(hash.AsSpan(0, 16));
    }
}
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockMate.Application.Common.Idempotency;
using StockMate.Infrastructure.Persistence;
using StockMate.Infrastructure.Persistence.Entities;

namespace StockMate.Infrastructure.Idempotency;

public sealed class IdempotencyService : IIdempotencyService
{
    private const string UniqueViolationCode = "23505";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly StockMateDbContext _dbContext;

    public IdempotencyService(StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Guid operationId,
        long telegramUserId,
        string operationType,
        object payload,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(
            operationId,
            telegramUserId,
            operationType,
            payload,
            operation);

        var payloadHash = CalculatePayloadHash(payload);

        var existingResult = await TryGetExistingResultAsync<TResult>(
            operationId,
            telegramUserId,
            operationType,
            payloadHash,
            cancellationToken);

        if (existingResult.Found)
            return existingResult.Result!;

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var processedOperation = new ProcessedOperation(
            operationId,
            telegramUserId,
            operationType,
            payloadHash);

        await _dbContext.ProcessedOperations.AddAsync(
            processedOperation,
            cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (IsUniqueViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);

            _dbContext.Entry(processedOperation).State =
                EntityState.Detached;

            var result = await TryGetExistingResultAsync<TResult>(
                operationId,
                telegramUserId,
                operationType,
                payloadHash,
                cancellationToken);

            if (!result.Found)
            {
                throw new InvalidOperationException(
                    "Не удалось получить результат ранее " +
                    "обработанной операции.",
                    exception);
            }

            return result.Result!;
        }

        try
        {
            var result = await operation(cancellationToken);

            processedOperation.Complete(
                JsonSerializer.Serialize(result, JsonOptions));

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ExecuteAsync(
        Guid operationId,
        long telegramUserId,
        string operationType,
        object payload,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            operationId,
            telegramUserId,
            operationType,
            payload,
            async token =>
            {
                await operation(token);
                return true;
            },
            cancellationToken);
    }

    private async Task<ExistingResult<TResult>>
        TryGetExistingResultAsync<TResult>(
            Guid operationId,
            long telegramUserId,
            string operationType,
            string payloadHash,
            CancellationToken cancellationToken)
    {
        var existing = await _dbContext.ProcessedOperations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                value =>
                    value.TelegramUserId == telegramUserId &&
                    value.OperationId == operationId,
                cancellationToken);

        if (existing is null)
            return ExistingResult<TResult>.NotFound();

        if (!string.Equals(
                existing.OperationType,
                operationType,
                StringComparison.Ordinal) ||
            !string.Equals(
                existing.PayloadHash,
                payloadHash,
                StringComparison.Ordinal))
        {
            throw new IdempotencyConflictException(operationId);
        }

        if (!existing.IsCompleted)
        {
            throw new InvalidOperationException(
                "Операция уже выполняется. Повторите попытку позже.");
        }

        if (string.IsNullOrWhiteSpace(existing.ResultJson))
            return ExistingResult<TResult>.FromResult(default!);

        var result = JsonSerializer.Deserialize<TResult>(
            existing.ResultJson,
            JsonOptions);

        return ExistingResult<TResult>.FromResult(result!);
    }

    private static string CalculatePayloadHash(object payload)
    {
        var json = JsonSerializer.Serialize(
            payload,
            payload.GetType(),
            JsonOptions);

        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(json));

        return Convert.ToHexString(hash);
    }

    private static bool IsUniqueViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: UniqueViolationCode
        };
    }

    private static void ValidateArguments(
        Guid operationId,
        long telegramUserId,
        string operationType,
        object payload,
        Delegate operation)
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

        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(operation);
    }

    private sealed record ExistingResult<TResult>(
        bool Found,
        TResult? Result)
    {
        public static ExistingResult<TResult> NotFound()
            => new(false, default);

        public static ExistingResult<TResult> FromResult(
            TResult result)
            => new(true, result);
    }
}
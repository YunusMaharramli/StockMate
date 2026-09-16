using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace StockMate.Bot.ErrorHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException =>
                StatusCodes.Status403Forbidden,

            KeyNotFoundException =>
                StatusCodes.Status404NotFound,

            ArgumentException =>
                StatusCodes.Status400BadRequest,

            InvalidOperationException =>
                StatusCodes.Status409Conflict,

            DbUpdateConcurrencyException =>
                StatusCodes.Status409Conflict,

            DbUpdateException =>
                StatusCodes.Status409Conflict,

            _ =>
                StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "An unexpected error occurred.");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode}.",
                statusCode);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = GetTitle(statusCode),
            Detail = GetDetail(exception, statusCode),
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static string GetDetail(
        Exception exception,
        int statusCode)
    {
        return exception switch
        {
            DbUpdateConcurrencyException =>
                "Данные были изменены другой операцией. Обновите страницу и повторите попытку.",

            DbUpdateException =>
                "The database operation conflicts with the current data.",

            _ when statusCode ==
                StatusCodes.Status500InternalServerError =>
                "An unexpected server error occurred.",

            _ => exception.Message
        };
    }

    private static string GetTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest =>
                "Неверный запрос",

            StatusCodes.Status404NotFound =>
                "Ресурс не найден",

            StatusCodes.Status409Conflict =>
                "Business rule conflict",

            StatusCodes.Status403Forbidden =>
                "Access denied",

            _ =>
                "Internal server error"
        };
    }
}

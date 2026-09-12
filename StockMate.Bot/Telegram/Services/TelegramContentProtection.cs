using Telegram.Bot.Requests.Abstractions;

namespace StockMate.Bot.Telegram.Services;

internal static class TelegramContentProtection
{
    private const string ProtectContentPropertyName =
        "ProtectContent";

    public static void Apply<TResponse>(
        IRequest<TResponse> request)
    {
        var property = request
            .GetType()
            .GetProperty(ProtectContentPropertyName);

        if (property is
            {
                CanWrite: true,
                PropertyType: not null
            } &&
            property.PropertyType == typeof(bool))
        {
            property.SetValue(request, true);
        }
    }
}

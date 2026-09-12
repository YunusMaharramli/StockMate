using Telegram.Bot;
using Telegram.Bot.Args;

namespace StockMate.Bot.Telegram.Services;

internal static class TelegramContentProtection
{
    private const string ProtectContentPropertyName =
        "ProtectContent";

    public static ValueTask ApplyAsync(
        ITelegramBotClient botClient,
        ApiRequestEventArgs args,
        CancellationToken cancellationToken)
    {
        var property = args.Request
            .GetType()
            .GetProperty(ProtectContentPropertyName);

        if (property is
            {
                CanWrite: true,
                PropertyType: not null
            } &&
            property.PropertyType == typeof(bool))
        {
            property.SetValue(args.Request, true);
        }

        return ValueTask.CompletedTask;
    }
}

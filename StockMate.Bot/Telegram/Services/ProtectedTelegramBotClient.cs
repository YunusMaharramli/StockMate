using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;

namespace StockMate.Bot.Telegram.Services;

public sealed class ProtectedTelegramBotClient : TelegramBotClient
{
    public ProtectedTelegramBotClient(string token)
        : base(token)
    {
    }

    public override Task<TResponse> SendRequest<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        TelegramContentProtection.Apply(request);

        return base.SendRequest(request, cancellationToken);
    }
}

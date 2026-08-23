namespace StockMate.Bot.Options;

public class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;

    public long AdminUserId { get; init; }
}
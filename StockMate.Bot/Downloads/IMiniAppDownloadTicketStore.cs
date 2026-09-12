namespace StockMate.Bot.Downloads;

public interface IMiniAppDownloadTicketStore
{
    string Create(TimeSpan lifetime);

    bool TryConsume(string token);
}

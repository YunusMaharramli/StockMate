using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Security;

public interface ITelegramInitDataValidator
{
    TelegramMiniAppUser Validate(string initData);
}
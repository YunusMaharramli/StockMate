using Microsoft.Extensions.Options;
using StockMate.Application.Notifications;
using StockMate.Application.Persistence;
using StockMate.Bot.Options;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Telegram.Services;

public sealed class UserAccessChangedNotifier(
    IStaffRepository staffRepository,
    ICustomerRepository customerRepository,
    IMiniAppMenuButtonService menuButtons,
    IOptions<TelegramOptions> options) : IUserAccessChangedNotifier
{
    public async Task NotifyAsync(long telegramUserId, CancellationToken cancellationToken = default)
    {
        if (telegramUserId == options.Value.AdminUserId)
        {
            await menuButtons.SetAsync(telegramUserId, MiniAppUserRole.Admin, cancellationToken);
            return;
        }

        var staff = await staffRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (staff is not null)
        {
            if (staff.IsActive && staff.Role == StaffRole.Worker)
                await menuButtons.SetAsync(telegramUserId, MiniAppUserRole.Worker, cancellationToken);
            else
                await menuButtons.ResetAsync(telegramUserId, cancellationToken);
            return;
        }

        var customer = await customerRepository.GetByTelegramUserIdAsync(telegramUserId, cancellationToken);
        if (customer is { IsBlocked: false })
            await menuButtons.SetAsync(telegramUserId, MiniAppUserRole.Customer, cancellationToken);
        else
            await menuButtons.ResetAsync(telegramUserId, cancellationToken);
    }
}

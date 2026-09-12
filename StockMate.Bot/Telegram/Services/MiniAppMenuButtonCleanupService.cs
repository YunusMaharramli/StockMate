using Microsoft.Extensions.Options;
using StockMate.Application.Customers.Services;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Options;

namespace StockMate.Bot.Telegram.Services;

public sealed class MiniAppMenuButtonCleanupService(
    IServiceScopeFactory scopeFactory,
    IMiniAppMenuButtonService menuButtons,
    IOptions<TelegramOptions> options,
    ILogger<MiniAppMenuButtonCleanupService> logger)
    : IHostedService
{
    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await menuButtons.ResetDefaultAsync(cancellationToken);

        await using var scope = scopeFactory.CreateAsyncScope();
        var customers = scope.ServiceProvider
            .GetRequiredService<ICustomerService>();
        var staff = scope.ServiceProvider
            .GetRequiredService<IStaffService>();

        var customerIds = (await customers.GetAllAsync(cancellationToken))
            .Select(customer => customer.TelegramUserId);
        var staffIds = (await staff.GetAllAsync(cancellationToken))
            .Select(member => member.TelegramUserId);

        var telegramUserIds = customerIds
            .Concat(staffIds)
            .Append(options.Value.AdminUserId)
            .Where(telegramUserId => telegramUserId > 0)
            .Distinct()
            .ToArray();

        foreach (var telegramUserId in telegramUserIds)
        {
            await menuButtons.ResetAsync(
                telegramUserId,
                cancellationToken);
        }

        logger.LogInformation(
            "Removed persistent Mini App menu buttons for {UserCount} Telegram users.",
            telegramUserIds.Length);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

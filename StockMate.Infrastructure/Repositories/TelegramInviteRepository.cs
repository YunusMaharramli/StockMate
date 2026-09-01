using Microsoft.EntityFrameworkCore;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Infrastructure.Persistence;

namespace StockMate.Infrastructure.Repositories;

public sealed class TelegramInviteRepository
    : ITelegramInviteRepository
{
    private readonly StockMateDbContext _dbContext;

    public TelegramInviteRepository(
        StockMateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TelegramInvite?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TelegramInvites
            .FirstOrDefaultAsync(
                invite => invite.Token == token,
                cancellationToken);
    }

    public async Task AddAsync(
        TelegramInvite invite,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TelegramInvites.AddAsync(
            invite,
            cancellationToken);
    }
    public async Task<bool> TryUseAsync(
    string token,
    long telegramUserId,
    CancellationToken cancellationToken = default)
    {
        var invite = await _dbContext.TelegramInvites
            .FirstOrDefaultAsync(
                invite =>
                    invite.Token == token &&
                    invite.UsedAtUtc == null,
                cancellationToken);

        if (invite is null)
        {
            return false;
        }

        invite.Use(telegramUserId);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.Entry(invite).State = EntityState.Detached;
            return false;
        }
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
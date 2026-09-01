using System.Security.Cryptography;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;

namespace StockMate.Application.TelegramInvites.Services;

public sealed class TelegramInviteService
    : ITelegramInviteService
{
    private readonly ITelegramInviteRepository
        _telegramInviteRepository;

    public TelegramInviteService(
        ITelegramInviteRepository telegramInviteRepository)
    {
        _telegramInviteRepository = telegramInviteRepository;
    }

    public async Task<string> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(
            RandomNumberGenerator.GetBytes(24));

        var invite = new TelegramInvite(token);

        await _telegramInviteRepository.AddAsync(
            invite,
            cancellationToken);

        await _telegramInviteRepository.SaveChangesAsync(
            cancellationToken);

        return token;
    }

    public Task<bool> TryUseAsync(
        string token,
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(false);
        }

        return _telegramInviteRepository.TryUseAsync(
            token.Trim(),
            telegramUserId,
            cancellationToken);
    }
}
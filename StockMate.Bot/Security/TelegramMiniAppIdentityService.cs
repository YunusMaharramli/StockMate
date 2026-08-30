using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StockMate.Application.Staff.Models;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Options;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Security;

public class TelegramMiniAppIdentityService : IMiniAppIdentityService
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromHours(24);
    private readonly IStaffService _staffService;
    private readonly TelegramOptions _options;

    public TelegramMiniAppIdentityService(
        IStaffService staffService,
        IOptions<TelegramOptions> options)
    {
        _staffService = staffService;
        _options = options.Value;
    }

    public async Task<StaffDto> RequireAsync(
        string initData,
        StaffRole? requiredRole = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(initData))
            throw new UnauthorizedAccessException("Telegram initData is required.");

        var values = QueryHelpers.ParseQuery(initData)
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

        if (!values.Remove("hash", out var receivedHash) ||
            string.IsNullOrWhiteSpace(receivedHash))
            throw new UnauthorizedAccessException("Telegram hash is missing.");

        var dataCheckString = string.Join(
            "\n",
            values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(_options.BotToken));
        var calculatedHash = HMACSHA256.HashData(
            secretKey,
            Encoding.UTF8.GetBytes(dataCheckString));

        byte[] receivedHashBytes;
        try { receivedHashBytes = Convert.FromHexString(receivedHash); }
        catch (FormatException) { throw new UnauthorizedAccessException("Telegram hash is invalid."); }

        if (!CryptographicOperations.FixedTimeEquals(calculatedHash, receivedHashBytes))
            throw new UnauthorizedAccessException("Telegram signature is invalid.");

        if (!values.TryGetValue("auth_date", out var authDateText) ||
            !long.TryParse(authDateText, NumberStyles.None, CultureInfo.InvariantCulture, out var authDate))
            throw new UnauthorizedAccessException("Telegram auth_date is invalid.");

        var createdAt = DateTimeOffset.FromUnixTimeSeconds(authDate);
        if (DateTimeOffset.UtcNow - createdAt > MaximumAge || createdAt > DateTimeOffset.UtcNow.AddMinutes(1))
            throw new UnauthorizedAccessException("Telegram initData has expired.");

        if (!values.TryGetValue("user", out var userJson))
            throw new UnauthorizedAccessException("Telegram user is missing.");

        using var userDocument = JsonDocument.Parse(userJson);
        var telegramUserId = userDocument.RootElement.GetProperty("id").GetInt64();
        var staff = await _staffService.GetByTelegramUserIdAsync(telegramUserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User is not a staff member.");

        if (!staff.IsActive)
            throw new UnauthorizedAccessException("Staff member is inactive.");
        if (requiredRole.HasValue && staff.Role != requiredRole.Value)
            throw new UnauthorizedAccessException("Staff member does not have the required role.");

        return staff;
    }
}

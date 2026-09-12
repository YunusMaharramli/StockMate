using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using StockMate.Bot.Options;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Security;

public sealed class TelegramInitDataValidator
    : ITelegramInitDataValidator
{
    private static readonly TimeSpan MaximumAge =
        TimeSpan.FromHours(24);

    private readonly TelegramOptions _options;

    public TelegramInitDataValidator(
        IOptions<TelegramOptions> options)
    {
        _options = options.Value;
    }

    public TelegramMiniAppUser Validate(string initData)
    {
        if (string.IsNullOrWhiteSpace(initData))
            throw AccessDenied();

        var values = QueryHelpers
            .ParseQuery(initData)
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToString());

        if (!values.Remove("hash", out var receivedHash) ||
            string.IsNullOrWhiteSpace(receivedHash))
        {
            throw AccessDenied();
        }

        ValidateSignature(values, receivedHash);
        ValidateAuthenticationDate(values);

        return ReadTelegramUser(values);
    }

    private void ValidateSignature(
        IReadOnlyDictionary<string, string> values,
        string receivedHash)
    {
        var dataCheckString = string.Join(
            "\n",
            values
                .OrderBy(
                    pair => pair.Key,
                    StringComparer.Ordinal)
                .Select(
                    pair => $"{pair.Key}={pair.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(_options.BotToken));

        var calculatedHash = HMACSHA256.HashData(
            secretKey,
            Encoding.UTF8.GetBytes(dataCheckString));

        byte[] receivedHashBytes;

        try
        {
            receivedHashBytes =
                Convert.FromHexString(receivedHash);
        }
        catch (FormatException)
        {
            throw AccessDenied();
        }

        if (!CryptographicOperations.FixedTimeEquals(
                calculatedHash,
                receivedHashBytes))
        {
            throw AccessDenied();
        }
    }

    private static void ValidateAuthenticationDate(
        IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue(
                "auth_date",
                out var authDateText) ||
            !long.TryParse(
                authDateText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var authDate))
        {
            throw AccessDenied();
        }

        DateTimeOffset createdAt;

        try
        {
            createdAt =
                DateTimeOffset.FromUnixTimeSeconds(authDate);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw AccessDenied();
        }

        var currentTime = DateTimeOffset.UtcNow;

        if (createdAt > currentTime.AddMinutes(1) ||
            currentTime - createdAt > MaximumAge)
        {
            throw AccessDenied();
        }
    }

    private static TelegramMiniAppUser ReadTelegramUser(
        IReadOnlyDictionary<string, string> values)
    {
        if (!values.TryGetValue(
                "user",
                out var userJson) ||
            string.IsNullOrWhiteSpace(userJson))
        {
            throw AccessDenied();
        }

        try
        {
            using var document =
                JsonDocument.Parse(userJson);

            var root = document.RootElement;

            if (!root.TryGetProperty(
                    "id",
                    out var idProperty) ||
                !idProperty.TryGetInt64(
                    out var telegramUserId) ||
                telegramUserId <= 0)
            {
                throw AccessDenied();
            }

            if (!root.TryGetProperty(
                    "first_name",
                    out var firstNameProperty))
            {
                throw AccessDenied();
            }

            var firstName =
                firstNameProperty.GetString();

            if (string.IsNullOrWhiteSpace(firstName))
                throw AccessDenied();

            return new TelegramMiniAppUser(
                telegramUserId,
                firstName,
                ReadOptionalString(root, "last_name"),
                ReadOptionalString(root, "username"));
        }
        catch (JsonException)
        {
            throw AccessDenied();
        }
        catch (InvalidOperationException)
        {
            throw AccessDenied();
        }
    }

    private static string? ReadOptionalString(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    private static UnauthorizedAccessException AccessDenied()
    {
        return new UnauthorizedAccessException(
            "İcazəniz yoxdur.");
    }
}
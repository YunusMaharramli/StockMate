using StockMate.Domain.Enums;

namespace StockMate.Domain.Entities;

public class Staff
{
    private Staff()
    {
    }

    public Staff(
        long telegramUserId,
        string firstName,
        string? lastName,
        string? username,
        StaffRole role)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(telegramUserId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "First name is required.",
                nameof(firstName));
        }

        Id = Guid.NewGuid();
        TelegramUserId = telegramUserId;
        FirstName = firstName.Trim();
        LastName = string.IsNullOrWhiteSpace(lastName)
            ? null
            : lastName.Trim();

        Username = string.IsNullOrWhiteSpace(username)
            ? null
            : username.Trim().TrimStart('@');

        Role = role;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public long TelegramUserId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string? LastName { get; private set; }

    public string? Username { get; private set; }

    public StaffRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
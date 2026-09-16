namespace StockMate.Domain.Entities;

public class Customer
{
    private static readonly int[] AllowedPageSizes = [5, 10, 20];

    private Customer()
    {
    }

    public Customer(
        long telegramUserId,
        string firstName,
        string? lastName,
        string? username)
    {
        if (telegramUserId <= 0)
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Имя обязательно.", nameof(firstName));

        Id = Guid.NewGuid();
        TelegramUserId = telegramUserId;
        FirstName = firstName.Trim();
        LastName = lastName?.Trim();
        Username = username?.Trim();
        ProductPageSize = 5;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public long TelegramUserId { get; private set; }

    public string FirstName { get; private set; } = string.Empty;

    public string? LastName { get; private set; }

    public string? Username { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? DeliveryAddress { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public int ProductPageSize { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsBlocked { get; private set; }

    public DateTime? BlockedAtUtc { get; private set; }

    public void UpdateProfile(
        string firstName,
        string? lastName,
        string? username)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("Имя обязательно.", nameof(firstName));

        FirstName = firstName.Trim();
        LastName = lastName?.Trim();
        Username = username?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateContactInformation(
        string phoneNumber,
        string deliveryAddress,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Номер телефона обязателен.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new ArgumentException("Адрес доставки обязателен.", nameof(deliveryAddress));

        PhoneNumber = phoneNumber.Trim();
        DeliveryAddress = deliveryAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeProductPageSize(int pageSize)
    {
        if (!AllowedPageSizes.Contains(pageSize))
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Размер страницы должен быть 5, 10 или 20.");

        ProductPageSize = pageSize;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) ||
            phoneNumber.Count(char.IsDigit) < 9)
        {
            throw new ArgumentException(
                "Введите номер телефона в правильном формате.",
                nameof(phoneNumber));
        }

        PhoneNumber = phoneNumber.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Block()
    {
        if (IsBlocked)
            return;

        IsBlocked = true;
        BlockedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        if (!IsBlocked)
            return;

        IsBlocked = false;
        BlockedAtUtc = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

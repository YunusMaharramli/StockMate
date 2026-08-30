namespace StockMate.Domain.Entities;

public sealed class CustomerAddress
{
    private CustomerAddress()
    {
    }

    public CustomerAddress(
        Guid customerId,
        string addressLine,
        double? latitude = null,
        double? longitude = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException(
                "Customer ID is required.",
                nameof(customerId));

        if (string.IsNullOrWhiteSpace(addressLine))
            throw new ArgumentException(
                "Address is required.",
                nameof(addressLine));

        Id = Guid.NewGuid();
        CustomerId = customerId;
        AddressLine = addressLine.Trim();
        Latitude = latitude;
        Longitude = longitude;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public string AddressLine { get; private set; } = string.Empty;

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void Update(
        string addressLine,
        double? latitude = null,
        double? longitude = null)
    {
        if (string.IsNullOrWhiteSpace(addressLine))
            throw new ArgumentException(
                "Address is required.",
                nameof(addressLine));

        AddressLine = addressLine.Trim();
        Latitude = latitude;
        Longitude = longitude;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
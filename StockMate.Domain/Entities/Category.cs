namespace StockMate.Domain.Entities;

public sealed class Category
{
    private Category()
    {
    }

    public Category(string name, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));

        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder));

        Id = Guid.NewGuid();
        SetName(name);
        SortOrder = sortOrder;
        IsActive = true;
        IsArchived = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsArchived { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));

        SetName(name);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder));

        SortOrder = sortOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Archived category cannot be activated.");

        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        IsArchived = true;
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        IsArchived = false;
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetName(string name)
    {
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
    }
}
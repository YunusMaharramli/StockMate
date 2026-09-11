namespace StockMate.Bot.Telegram.State;

public class TelegramSession
{
    public CustomerInputStep CustomerInputStep { get; set; }

    public Guid? CustomerProductId { get; set; }

    public CheckoutStep CheckoutStep { get; set; }

    public string? PhoneNumber { get; set; }

    public string? DeliveryAddress { get; set; }

    public Guid? CustomerAddressId { get; set; }

    public Guid? DeliveryCompanyId { get; set; }
    public AdminInputStep AdminInputStep { get; set; }

    public AdminStockAction AdminStockAction { get; set; }

    public Guid? ProductId { get; set; }

    public int? StockQuantity { get; set; }
    public bool ContinueToCheckout { get; set; }
    public Guid? ProductCategoryId { get; set; }
    public string? ProductSubcategoryName { get; set; }
    public string? ProductSearch { get; set; }
    public int ProductCatalogPage { get; set; } = 1;
    public long? WorkerTelegramUserId { get; set; }
    public string? WorkerFirstName { get; set; }
    public string? WorkerLastName { get; set; }
}

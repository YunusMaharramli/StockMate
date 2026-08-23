namespace StockMate.Bot.Telegram.State;

public class TelegramSession
{
    public CheckoutStep CheckoutStep { get; set; }

    public string? PhoneNumber { get; set; }

    public string? DeliveryAddress { get; set; }
    public AdminInputStep AdminInputStep { get; set; }

    public AdminStockAction AdminStockAction { get; set; }

    public Guid? ProductId { get; set; }

    public int? StockQuantity { get; set; }
    public string? ProductName { get; set; }

    public string? ProductCode { get; set; }

    public decimal? ProductPrice { get; set; }

    public int? InitialStockQuantity { get; set; }

    public string? ProductDescription { get; set; }
    public bool ContinueToCheckout { get; set; }
}
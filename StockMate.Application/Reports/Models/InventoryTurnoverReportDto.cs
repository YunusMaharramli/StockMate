namespace StockMate.Application.Reports.Models;

public sealed record InventoryTurnoverReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? DeliveryCompanyId,
    string? DeliveryCompanyName,
    IReadOnlyList<ProductTurnoverDto> Products,
    int TotalPurchasedQuantity,
    decimal TotalPurchaseAmount,
    int TotalSoldQuantity,
    decimal TotalSalesAmount);
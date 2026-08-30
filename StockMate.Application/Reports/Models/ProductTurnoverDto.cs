namespace StockMate.Application.Reports.Models;

public sealed record ProductTurnoverDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int PurchasedQuantity,
    decimal AveragePurchaseUnitCost,
    decimal PurchaseAmount,
    int SoldQuantity,
    decimal AverageSaleUnitPrice,
    decimal SalesAmount);
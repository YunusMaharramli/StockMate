namespace StockMate.Application.Products.Models;

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    string? ImageReference);

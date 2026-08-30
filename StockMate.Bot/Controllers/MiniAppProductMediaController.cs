using Microsoft.AspNetCore.Mvc;
using StockMate.Application.Products.Services;
using StockMate.Bot.Security;
using StockMate.Domain.Enums;
using Telegram.Bot;

namespace StockMate.Bot.Controllers;

[ApiController]
public sealed class MiniAppProductMediaController : ControllerBase
{
    private const int MaximumImageSize = 5 * 1024 * 1024;
    private readonly IMiniAppIdentityService _identity;
    private readonly IProductService _products;
    private readonly TelegramBotClient _botClient;
    private readonly IWebHostEnvironment _environment;

    public MiniAppProductMediaController(
        IMiniAppIdentityService identity,
        IProductService products,
        TelegramBotClient botClient,
        IWebHostEnvironment environment)
    {
        _identity = identity;
        _products = products;
        _botClient = botClient;
        _environment = environment;
    }

    [HttpPost("api/miniapp/admin/products/images")]
    [RequestSizeLimit(MaximumImageSize)]
    public async Task<IActionResult> Upload(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        await _identity.RequireAsync(
            Request.Headers["X-Telegram-Init-Data"].ToString(),
            StaffRole.Admin,
            cancellationToken);

        if (image.Length is <= 0 or > MaximumImageSize)
            throw new ArgumentException("Image must be smaller than 5 MB.");

        var extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };
        if (!extensions.TryGetValue(image.ContentType, out var extension))
            throw new ArgumentException("Only JPG, PNG and WEBP images are supported.");

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var directory = GetImageDirectory();
        Directory.CreateDirectory(directory);

        await using var stream = System.IO.File.Create(Path.Combine(directory, fileName));
        await image.CopyToAsync(stream, cancellationToken);
        return Ok(new { ImageReference = $"local:{fileName}" });
    }

    [HttpGet("api/product-images/{productId:guid}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Get(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(productId, cancellationToken);
        if (product is null || string.IsNullOrWhiteSpace(product.ImageReference))
            return NotFound();

        var reference = product.ImageReference;
        if (Uri.TryCreate(reference, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https")
            return Redirect(uri.ToString());

        if (reference.StartsWith("local:", StringComparison.Ordinal))
        {
            var fileName = Path.GetFileName(reference["local:".Length..]);
            var path = Path.Combine(GetImageDirectory(), fileName);
            return System.IO.File.Exists(path)
                ? PhysicalFile(path, GetContentType(path))
                : NotFound();
        }

        var telegramFile = await _botClient.GetFile(reference, cancellationToken);
        var memory = new MemoryStream();
        await _botClient.DownloadFile(telegramFile, memory, cancellationToken);
        memory.Position = 0;
        return File(memory, GetContentType(telegramFile.FilePath ?? string.Empty));
    }

    private string GetImageDirectory()
        => Path.Combine(_environment.ContentRootPath, "App_Data", "product-images");

    private static string GetContentType(string path)
        => Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
}

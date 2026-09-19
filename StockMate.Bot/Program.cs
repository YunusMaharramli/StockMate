using Microsoft.Extensions.Options;
using StockMate.Application;
using StockMate.Application.Notifications;
using StockMate.Bot.Downloads;
using StockMate.Bot.ErrorHandling;
using StockMate.Bot.Options;
using StockMate.Bot.Outbox;
using StockMate.Bot.Security;
using StockMate.Bot.Telegram.Handlers;
using StockMate.Bot.Telegram.Services;
using StockMate.Bot.Telegram.State;
using StockMate.Infrastructure;
using System.Text.Json.Serialization;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

// Console logging works in local development, containers and restricted hosts.
// The default Windows Event Log provider can fail when the process is not elevated.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddScoped<MiniAppAuthorizationFilter>();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.AddService<MiniAppAuthorizationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

var miniAppOrigins = builder.Configuration
    .GetSection("MiniApp:AllowedOrigins")
    .Get<string[]>() ?? ["http://127.0.0.1:4173", "http://localhost:4173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("MiniApp", policy => policy
        .WithOrigins(miniAppOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var connectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException(
        "PostgreSQL connection string was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

builder.Services.Configure<TelegramOptions>(
    builder.Configuration.GetSection(
        TelegramOptions.SectionName));


builder.Services.AddSingleton<TelegramBotClient>(
    serviceProvider =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<TelegramOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(options.BotToken) ||
            options.BotToken.Contains("your_bot_token", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Telegram bot token was not configured. Set Telegram:BotToken " +
                "in appsettings.Development.json or Telegram__BotToken in the environment.");
        }

        if (options.AdminUserId <= 0)
        {
            throw new InvalidOperationException(
                "Telegram admin user ID was not configured. Set Telegram:AdminUserId " +
                "in appsettings.Development.json or Telegram__AdminUserId in the environment.");
        }

        return new ProtectedTelegramBotClient(options.BotToken);
    });

builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramPollingService>();
builder.Services.AddSingleton<TelegramSessionStore>();
builder.Services.AddSingleton<IMiniAppMenuButtonService, MiniAppMenuButtonService>();
builder.Services.AddSingleton<IMiniAppDownloadTicketStore, MiniAppDownloadTicketStore>();
builder.Services.AddScoped<IUserAccessChangedNotifier, UserAccessChangedNotifier>();
builder.Services.AddScoped<IWorkflowNotificationService,WorkflowNotificationService>();
builder.Services.AddScoped<OutboxMessageDispatcher>();
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddSingleton<ITelegramInitDataValidator, TelegramInitDataValidator>();
builder.Services.AddScoped<IMiniAppIdentityService, TelegramMiniAppIdentityService>();

var app = builder.Build();

app.UseExceptionHandler();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("MiniApp");

app.UseAuthorization();

var legacyApiEnabled = builder.Configuration.GetValue<bool>("LegacyApi:Enabled");
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isAllowedApi = path.StartsWithSegments("/api/miniapp") ||
                       path.StartsWithSegments("/api/product-images");

    if (!legacyApiEnabled && path.StartsWithSegments("/api") && !isAllowedApi)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.MapControllers();

app.Run();

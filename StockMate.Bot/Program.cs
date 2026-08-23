using Microsoft.Extensions.Options;
using StockMate.Application;
using StockMate.Bot.ErrorHandling;
using StockMate.Bot.Options;
using StockMate.Bot.Telegram.Handlers;
using StockMate.Bot.Telegram.Services;
using StockMate.Bot.Telegram.State;
using StockMate.Infrastructure;
using System.Text.Json.Serialization;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    }); builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

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

        if (string.IsNullOrWhiteSpace(options.BotToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token was not configured.");
        }

        return new TelegramBotClient(options.BotToken);
    });

builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramPollingService>();
builder.Services.AddSingleton<TelegramSessionStore>();

var app = builder.Build();

app.UseExceptionHandler();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace StockMate.Infrastructure.Persistence;

public class StockMateDbContextFactory
    : IDesignTimeDbContextFactory<StockMateDbContext>
{
    public StockMateDbContext CreateDbContext(string[] args)
    {
        var appSettingsPath = FindAppSettingsPath();
        using var document = JsonDocument.Parse(
            File.ReadAllText(appSettingsPath));

        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("PostgreSql")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PostgreSQL connection string was not found.");
        }

        var optionsBuilder =
            new DbContextOptionsBuilder<StockMateDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new StockMateDbContext(optionsBuilder.Options);
    }

    private static string FindAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(
                currentDirectory,
                "StockMate.Bot",
                "appsettings.json"),
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "StockMate.Bot/appsettings.json was not found.");
    }
}

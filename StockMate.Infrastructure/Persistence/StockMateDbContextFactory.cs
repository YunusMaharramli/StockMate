using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockMate.Infrastructure.Persistence;

public class StockMateDbContextFactory
    : IDesignTimeDbContextFactory<StockMateDbContext>
{
    public StockMateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<StockMateDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=stockmate;" +
            "Username=stockmate;Password=design-time");

        return new StockMateDbContext(optionsBuilder.Options);
    }
}
using StockMate.Application.Shortages.Models;

namespace StockMate.Application.Shortages.Exports;

public interface IShortageExcelExporter
{
    byte[] Export(
        IReadOnlyCollection<MissingProductDto> products);
}
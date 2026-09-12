using ClosedXML.Excel;
using StockMate.Application.Shortages.Exports;
using StockMate.Application.Shortages.Models;

namespace StockMate.Infrastructure.Shortages.Exports;

public sealed class ShortageExcelExporter
    : IShortageExcelExporter
{
    public byte[] Export(
        IReadOnlyCollection<MissingProductDto> products)
    {
        ArgumentNullException.ThrowIfNull(products);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(
            "Çatışmayan məhsullar");

        worksheet.Cell(1, 1).Value = "Məhsul";
        worksheet.Cell(1, 2).Value = "Məhsul kodu";
        worksheet.Cell(1, 3).Value = "Çatışmayan miqdar";

        var header = worksheet.Range(1, 1, 1, 3);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#2563EB");
        header.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        var row = 2;

        foreach (var product in products
                     .OrderBy(product => product.ProductName))
        {
            worksheet.Cell(row, 1).Value =
                product.ProductName;
            worksheet.Cell(row, 2).Value =
                product.ProductCode;
            worksheet.Cell(row, 3).Value =
                product.MissingQuantity;

            row++;
        }

        worksheet.SheetView.FreezeRows(1);
        worksheet.Columns().AdjustToContents();

        if (row > 2)
        {
            worksheet.Range(1, 1, row - 1, 3)
                .SetAutoFilter();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}

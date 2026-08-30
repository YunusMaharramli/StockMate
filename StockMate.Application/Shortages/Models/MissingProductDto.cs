using System;
using System.Collections.Generic;
using System.Text;

namespace StockMate.Application.Shortages.Models;

public sealed record MissingProductDto(
    Guid ProductId,
    string ProductName,
    string ProductCode,
    int MissingQuantity,
    int AffectedOrderCount);

using StockMate.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockMate.Application.Shortages.Models;

public sealed record MissingProductOrderDto(
    long OrderNumber,
    Guid CustomerId,
    string CustomerName,
    int MissingQuantity,
    SpecialOrderStatus Status);

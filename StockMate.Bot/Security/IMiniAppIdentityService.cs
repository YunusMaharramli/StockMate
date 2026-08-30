using StockMate.Application.Staff.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Security;

public interface IMiniAppIdentityService
{
    Task<StaffDto> RequireAsync(
        string initData,
        StaffRole? requiredRole = null,
        CancellationToken cancellationToken = default);
}

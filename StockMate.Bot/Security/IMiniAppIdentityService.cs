using StockMate.Application.Staff.Models;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Security;

public interface IMiniAppIdentityService
{
    Task<MiniAppIdentity> AuthenticateAsync(
        string initData,
        CancellationToken cancellationToken = default);

    Task<MiniAppIdentity> RequireRoleAsync(
        string initData,
        MiniAppUserRole requiredRole,
        CancellationToken cancellationToken = default);

    Task<StaffDto> RequireAsync(
        string initData,
        StaffRole? requiredRole = null,
        CancellationToken cancellationToken = default);
}
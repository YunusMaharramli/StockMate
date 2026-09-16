using Microsoft.Extensions.Options;
using StockMate.Application.Customers.Models;
using StockMate.Application.Customers.Services;
using StockMate.Application.Staff.Models;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Options;
using StockMate.Bot.Security.Models;
using StockMate.Domain.Enums;

namespace StockMate.Bot.Security;

public sealed class TelegramMiniAppIdentityService
    : IMiniAppIdentityService
{
    private readonly ITelegramInitDataValidator _initDataValidator;
    private readonly IStaffService _staffService;
    private readonly ICustomerService _customerService;
    private readonly TelegramOptions _options;

    public TelegramMiniAppIdentityService(
        ITelegramInitDataValidator initDataValidator,
        IStaffService staffService,
        ICustomerService customerService,
        IOptions<TelegramOptions> options)
    {
        _initDataValidator = initDataValidator;
        _staffService = staffService;
        _customerService = customerService;
        _options = options.Value;
    }

    public async Task<MiniAppIdentity> AuthenticateAsync(
        string initData,
        CancellationToken cancellationToken = default)
    {
        var telegramUser =
            _initDataValidator.Validate(initData);

        var staff = await GetAuthorizedStaffAsync(
            telegramUser,
            cancellationToken);

        if (staff is not null)
            return MapStaffIdentity(staff);

        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUser.TelegramUserId,
                cancellationToken);

        if (customer is null || customer.IsBlocked)
            throw AccessDenied();

        return MapCustomerIdentity(customer);
    }

    public async Task<MiniAppIdentity> RequireRoleAsync(
        string initData,
        MiniAppUserRole requiredRole,
        CancellationToken cancellationToken = default)
    {
        var identity = await AuthenticateAsync(
            initData,
            cancellationToken);

        if (identity.Role != requiredRole)
            throw AccessDenied();

        return identity;
    }

    public async Task<StaffDto> RequireAsync(
        string initData,
        StaffRole? requiredRole = null,
        CancellationToken cancellationToken = default)
    {
        var telegramUser =
            _initDataValidator.Validate(initData);

        var staff = await GetAuthorizedStaffAsync(
            telegramUser,
            cancellationToken);

        if (staff is null)
            throw AccessDenied();

        if (requiredRole.HasValue &&
            staff.Role != requiredRole.Value)
        {
            throw AccessDenied();
        }

        return staff;
    }

    private async Task<StaffDto?> GetAuthorizedStaffAsync(
        TelegramMiniAppUser telegramUser,
        CancellationToken cancellationToken)
    {
        var staff =
            await _staffService.GetByTelegramUserIdAsync(
                telegramUser.TelegramUserId,
                cancellationToken);

        if (telegramUser.TelegramUserId ==
            _options.AdminUserId)
        {
            if (staff is null ||
                !staff.IsActive ||
                staff.Role != StaffRole.Admin)
            {
                throw AccessDenied();
            }

            return staff;
        }

        if (staff is null)
            return null;

        if (!staff.IsActive ||
            staff.Role != StaffRole.Worker)
        {
            throw AccessDenied();
        }

        return staff;
    }

    private static MiniAppIdentity MapStaffIdentity(
        StaffDto staff)
    {
        var role = staff.Role switch
        {
            StaffRole.Admin => MiniAppUserRole.Admin,
            StaffRole.Worker => MiniAppUserRole.Worker,
            _ => throw AccessDenied()
        };

        return new MiniAppIdentity(
            staff.TelegramUserId,
            role,
            staff.Id,
            null,
            staff.FirstName,
            staff.LastName,
            staff.Username);
    }

    private static MiniAppIdentity MapCustomerIdentity(
        CustomerDto customer)
    {
        return new MiniAppIdentity(
            customer.TelegramUserId,
            MiniAppUserRole.Customer,
            null,
            customer.Id,
            customer.FirstName,
            customer.LastName,
            customer.Username);
    }

    private static UnauthorizedAccessException AccessDenied()
    {
        return new UnauthorizedAccessException(
            "У вас нет доступа.");
    }
}
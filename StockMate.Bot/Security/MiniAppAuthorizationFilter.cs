using Microsoft.AspNetCore.Mvc.Filters;
using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Security;

public sealed class MiniAppAuthorizationFilter
    : IAsyncActionFilter
{
    private const string IdentityItemKey =
        "StockMate.MiniAppIdentity";

    private readonly IMiniAppIdentityService
        _identityService;

    public MiniAppAuthorizationFilter(
        IMiniAppIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var authorizeAttribute =
            context.ActionDescriptor.EndpointMetadata
                .OfType<MiniAppAuthorizeAttribute>()
                .LastOrDefault();

        if (authorizeAttribute is null)
        {
            await next();
            return;
        }

        var initData = context.HttpContext
            .Request
            .Headers["X-Telegram-Init-Data"]
            .ToString();

        var identity =
            await _identityService.AuthenticateAsync(
                initData,
                context.HttpContext.RequestAborted);

        if (!authorizeAttribute.AllowedRoles.Contains(
                identity.Role))
        {
            throw new UnauthorizedAccessException(
                "İcazəniz yoxdur.");
        }

        context.HttpContext.Items[IdentityItemKey] =
            identity;

        await next();
    }

    public static MiniAppIdentity GetIdentity(
        HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(
                IdentityItemKey,
                out var value) &&
            value is MiniAppIdentity identity)
        {
            return identity;
        }

        throw new UnauthorizedAccessException(
            "Mini App istifadəçisi müəyyən edilmədi.");
    }
}
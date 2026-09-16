using StockMate.Bot.Security.Models;

namespace StockMate.Bot.Security;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class MiniAppAuthorizeAttribute : Attribute
{
    public MiniAppAuthorizeAttribute(
        params MiniAppUserRole[] allowedRoles)
    {
        if (allowedRoles.Length == 0)
        {
            throw new ArgumentException(
                "Необходимо указать хотя бы одну роль Mini App.",
                nameof(allowedRoles));
        }

        AllowedRoles = allowedRoles;
    }

    public IReadOnlyCollection<MiniAppUserRole>
        AllowedRoles
    { get; }
}
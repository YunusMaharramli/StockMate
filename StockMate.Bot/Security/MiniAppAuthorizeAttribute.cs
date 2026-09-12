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
                "Ən azı bir Mini App rolu göstərilməlidir.",
                nameof(allowedRoles));
        }

        AllowedRoles = allowedRoles;
    }

    public IReadOnlyCollection<MiniAppUserRole>
        AllowedRoles
    { get; }
}
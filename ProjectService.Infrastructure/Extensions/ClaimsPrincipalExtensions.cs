using System.Security.Claims;
using ProjectService.Domain.Extensions;
using ProjectService.Infrastructure.Exceptions.Http;

namespace ProjectService.Infrastructure.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetId(this ClaimsPrincipal principal)
    {
        var userId = principal.Claims.FirstOrDefault(x => x.Type.EqualsIgnoreCase(ClaimTypes.NameIdentifier))?.Value;
        
        if (userId is null)
        {
            throw new UnauthorizedException("User id not found in claims");
        }
        
        return userId;
    }

    public static string? TryGetId(this ClaimsPrincipal principal)
    {
        try
        {
            return principal.GetId();
        }
        catch (UnauthorizedException)
        {
            return null;
        }
    }
}
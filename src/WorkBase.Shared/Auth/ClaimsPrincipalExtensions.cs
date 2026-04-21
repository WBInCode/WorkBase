using System.Security.Claims;

namespace WorkBase.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetTenantId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("tenant_id") ?? user.FindFirst("tenantId");
        return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
    }

    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
    }
}

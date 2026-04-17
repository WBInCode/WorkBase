using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.Auth;

public sealed class HttpContextTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenantService
{
    public Guid? TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id");
            if (claim is null || !Guid.TryParse(claim.Value, out var tenantId))
                return null;
            return tenantId;
        }
    }
}

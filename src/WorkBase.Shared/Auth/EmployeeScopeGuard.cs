using System.Security.Claims;

namespace WorkBase.Shared.Auth;

/// <summary>
/// Dane innego pracownika widzi tylko ten, kto ma uprawnienie zespołowe — własne widzi każdy.
/// </summary>
public static class EmployeeScopeGuard
{
    public static Guid? EmployeeId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("employee_id"), out var id) ? id : null;

    public static async Task<bool> CanAccessEmployeeAsync(
        this ClaimsPrincipal user,
        Guid targetEmployeeId,
        IPermissionService permissions,
        string teamPermission,
        CancellationToken ct = default)
    {
        if (user.EmployeeId() == targetEmployeeId) return true;
        return await user.HasPermissionAsync(permissions, teamPermission, ct);
    }

    public static async Task<bool> HasPermissionAsync(
        this ClaimsPrincipal user,
        IPermissionService permissions,
        string permission,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(user.FindFirstValue("sub"), out var userId)
            || !Guid.TryParse(user.FindFirstValue("tenant_id"), out var tenantId))
        {
            return false;
        }

        return await permissions.HasPermissionAsync(userId, tenantId, permission, ct);
    }
}

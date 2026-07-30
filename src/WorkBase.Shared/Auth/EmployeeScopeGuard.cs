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
        IEmployeeScopeResolver scopes,
        string teamPermission,
        string module,
        CancellationToken ct = default)
    {
        if (user.EmployeeId() == targetEmployeeId) return true;
        if (!await user.HasPermissionAsync(permissions, teamPermission, ct)) return false;
        if (!user.TryGetIdentity(out var userId, out var tenantId)) return false;

        return await scopes.CanAccessEmployeeAsync(userId, tenantId, user.EmployeeId(), targetEmployeeId, module, ct);
    }

    public static async Task<IReadOnlySet<Guid>> FilterAccessibleEmployeesAsync(
        this ClaimsPrincipal user,
        IReadOnlyCollection<Guid> targetEmployeeIds,
        IPermissionService permissions,
        IEmployeeScopeResolver scopes,
        string teamPermission,
        string module,
        CancellationToken ct = default)
    {
        var ownEmployeeId = user.EmployeeId();
        var foreignIds = targetEmployeeIds.Where(id => id != ownEmployeeId).ToList();
        var accessible = targetEmployeeIds.Where(id => id == ownEmployeeId).ToHashSet();

        if (foreignIds.Count == 0) return accessible;
        if (!await user.HasPermissionAsync(permissions, teamPermission, ct)) return accessible;
        if (!user.TryGetIdentity(out var userId, out var tenantId)) return accessible;

        accessible.UnionWith(await scopes.FilterAccessibleAsync(userId, tenantId, ownEmployeeId, foreignIds, module, ct));
        return accessible;
    }

    public static async Task<bool> HasPermissionAsync(
        this ClaimsPrincipal user,
        IPermissionService permissions,
        string permission,
        CancellationToken ct = default)
    {
        if (!user.TryGetIdentity(out var userId, out var tenantId)) return false;

        return await permissions.HasPermissionAsync(userId, tenantId, permission, ct);
    }

    private static bool TryGetIdentity(this ClaimsPrincipal user, out Guid userId, out Guid tenantId)
    {
        tenantId = Guid.Empty;
        return Guid.TryParse(user.FindFirstValue("sub"), out userId)
            && Guid.TryParse(user.FindFirstValue("tenant_id"), out tenantId);
    }
}

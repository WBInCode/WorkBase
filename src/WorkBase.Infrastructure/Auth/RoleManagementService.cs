using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

public sealed class RoleManagementService(WorkBaseDbContext dbContext) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(Guid tenantId, CancellationToken ct = default)
    {
        var rows = await dbContext.Set<Role>()
            .Where(r => r.TenantId == tenantId)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Type,
                r.IsActive,
                r.Level,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count
            })
            .OrderBy(r => r.Level)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

        return rows.Select(r => new RoleDto(
            r.Id, r.Name, r.Description, r.Type.ToString(),
            r.IsActive, r.Level, r.PermissionCount, r.UserCount)).ToList();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        var row = await dbContext.Set<Role>()
            .Where(r => r.Id == roleId)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description,
                r.Type,
                r.IsActive,
                r.Level,
                PermissionCount = r.RolePermissions.Count,
                UserCount = r.UserRoles.Count
            })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : new RoleDto(
            row.Id, row.Name, row.Description, row.Type.ToString(),
            row.IsActive, row.Level, row.PermissionCount, row.UserCount);
    }

    public async Task<Guid> CreateRoleAsync(Guid tenantId, string name, string? description, int level, CancellationToken ct = default)
    {
        var role = Role.Create(tenantId, name, RoleType.Custom, level, description);
        dbContext.Set<Role>().Add(role);
        await dbContext.SaveChangesAsync(ct);
        return role.Id;
    }

    public async Task UpdateRoleAsync(Guid roleId, string name, string? description, int level, CancellationToken ct = default)
    {
        var role = await dbContext.Set<Role>().FindAsync([roleId], ct)
            ?? throw new InvalidOperationException($"Role {roleId} not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot edit system roles.");

        role.Update(name, description, level);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await dbContext.Set<Role>().FindAsync([roleId], ct)
            ?? throw new InvalidOperationException($"Role {roleId} not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot delete system roles.");

        var permissions = await dbContext.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId).ToListAsync(ct);
        dbContext.Set<RolePermission>().RemoveRange(permissions);

        var userRoles = await dbContext.Set<UserRole>()
            .Where(ur => ur.RoleId == roleId).ToListAsync(ct);
        dbContext.Set<UserRole>().RemoveRange(userRoles);

        dbContext.Set<Role>().Remove(role);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default)
    {
        return await dbContext.Set<Permission>()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Action)
            .ThenBy(p => p.Scope)
            .Select(p => new PermissionDto(
                p.Id,
                p.Module,
                p.Action,
                p.Scope,
                p.Description,
                p.FullCode))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken ct = default)
    {
        return await dbContext.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);
    }

    public async Task UpdateRolePermissionsAsync(Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken ct = default)
    {
        var role = await dbContext.Set<Role>().FindAsync([roleId], ct)
            ?? throw new InvalidOperationException($"Role {roleId} not found.");

        if (role.IsSystemRole)
            throw new InvalidOperationException("Cannot modify permissions of system roles.");

        var existing = await dbContext.Set<RolePermission>()
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(ct);

        dbContext.Set<RolePermission>().RemoveRange(existing);

        var newEntries = permissionIds
            .Distinct()
            .Select(pid => RolePermission.Create(roleId, pid));

        dbContext.Set<RolePermission>().AddRange(newEntries);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RoleUserDto>> GetRoleUsersAsync(
        Guid roleId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var roleBelongsToTenant = await dbContext.Set<Role>()
            .AnyAsync(role => role.Id == roleId && role.TenantId == tenantId, ct);

        if (!roleBelongsToTenant)
            return [];

        return await dbContext.Set<UserRole>()
            .Where(userRole => userRole.RoleId == roleId && userRole.TenantId == tenantId)
            .Join(
                dbContext.Set<User>().Where(user => user.TenantId == tenantId),
                userRole => userRole.UserId,
                user => user.Id,
                (userRole, user) => new { UserRole = userRole, User = user })
            .OrderBy(row => row.User.LastName)
            .ThenBy(row => row.User.FirstName)
            .ThenBy(row => row.User.Email)
            .Select(row => new RoleUserDto(
                row.User.Id,
                row.User.Email,
                row.User.FirstName,
                row.User.LastName,
                row.User.IsActive,
                row.UserRole.AssignedAt,
                row.UserRole.AssignedBy))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        // userId may be either the internal User.Id or the Keycloak sub (parsed as Guid) —
        // brokered/SSO logins carry the Keycloak id in `sub`, whereas iam_user_roles.user_id
        // stores the internal User.Id. Resolve to internal id so the UserRole join matches
        // (mirrors PermissionService.GetUserPermissionsAsync). Without this, isAdmin in
        // /api/auth/me was always false for brokered users → admin nav section hidden.
        var internalUserId = await dbContext.Set<User>()
            .Where(u => u.Id == userId || u.KeycloakId == userId.ToString())
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        if (internalUserId == Guid.Empty)
            return [];

        return await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == internalUserId && ur.TenantId == tenantId)
            .Join(
                dbContext.Set<Role>(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new UserRoleDto(
                    r.Id,
                    r.Name,
                    r.Type.ToString(),
                    ur.AssignedAt,
                    ur.AssignedBy))
            .ToListAsync(ct);
    }

    public async Task AssignUserRoleAsync(Guid userId, Guid roleId, Guid tenantId, string? assignedBy, CancellationToken ct = default)
    {
        var exists = await dbContext.Set<UserRole>()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId, ct);

        if (exists)
            return;

        var userRole = UserRole.Create(userId, roleId, tenantId, assignedBy);
        dbContext.Set<UserRole>().Add(userRole);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task UnassignUserRoleAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken ct = default)
    {
        var role = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException("Rola nie istnieje w bieżącej organizacji.");

        var userRole = await dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId, ct);

        if (userRole is null)
            return;

        if (userRole.AssignedBy == "system")
            throw new InvalidOperationException("Ta rola jest synchronizowana automatycznie z WB Platform. Zmień rolę lub dostęp użytkownika w panelu organizacji WB Platform.");

        if (role.Name == "Super Admin")
        {
            var superAdminCount = await dbContext.Set<UserRole>()
                .CountAsync(ur => ur.RoleId == roleId && ur.TenantId == tenantId, ct);

            if (superAdminCount <= 1)
                throw new InvalidOperationException("Nie można odebrać roli ostatniemu Super Adminowi.");
        }

        dbContext.Set<UserRole>().Remove(userRole);
        await dbContext.SaveChangesAsync(ct);
    }
}

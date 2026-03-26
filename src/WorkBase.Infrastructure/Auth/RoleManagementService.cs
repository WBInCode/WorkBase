using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;
using WorkBase.Shared.Auth;

namespace WorkBase.Infrastructure.Auth;

public sealed class RoleManagementService(WorkBaseDbContext dbContext) : IRoleManagementService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await dbContext.Set<Role>()
            .Where(r => r.TenantId == tenantId)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.Type.ToString(),
                r.IsActive,
                r.Level,
                r.RolePermissions.Count,
                r.UserRoles.Count))
            .OrderBy(r => r.Level)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        return await dbContext.Set<Role>()
            .Where(r => r.Id == roleId)
            .Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.Type.ToString(),
                r.IsActive,
                r.Level,
                r.RolePermissions.Count,
                r.UserRoles.Count))
            .FirstOrDefaultAsync(ct);
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

    public async Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        return await dbContext.Set<UserRole>()
            .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
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
        var userRole = await dbContext.Set<UserRole>()
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.TenantId == tenantId, ct);

        if (userRole is null)
            return;

        dbContext.Set<UserRole>().Remove(userRole);
        await dbContext.SaveChangesAsync(ct);
    }
}

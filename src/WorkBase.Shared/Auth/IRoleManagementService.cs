namespace WorkBase.Shared.Auth;

public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(Guid tenantId, CancellationToken ct = default);
    Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default);
    Task<Guid> CreateRoleAsync(Guid tenantId, string name, string? description, int level, CancellationToken ct = default);
    Task UpdateRoleAsync(Guid roleId, string name, string? description, int level, CancellationToken ct = default);
    Task DeleteRoleAsync(Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetRolePermissionIdsAsync(Guid roleId, CancellationToken ct = default);
    Task UpdateRolePermissionsAsync(Guid roleId, IReadOnlyList<Guid> permissionIds, CancellationToken ct = default);
    Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(Guid userId, Guid tenantId, CancellationToken ct = default);
    Task AssignUserRoleAsync(Guid userId, Guid roleId, Guid tenantId, string? assignedBy, CancellationToken ct = default);
    Task UnassignUserRoleAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken ct = default);
}

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    bool IsActive,
    int Level,
    int PermissionCount,
    int UserCount);

public sealed record PermissionDto(
    Guid Id,
    string Module,
    string Action,
    string? Scope,
    string? Description,
    string FullCode);

public sealed record UserRoleDto(
    Guid RoleId,
    string RoleName,
    string RoleType,
    DateTime AssignedAt,
    string? AssignedBy);

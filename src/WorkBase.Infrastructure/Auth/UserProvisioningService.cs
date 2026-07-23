using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Identity.Domain.Entities;

namespace WorkBase.Infrastructure.Auth;

public sealed class UserProvisioningService
{
    private readonly WorkBaseDbContext _dbContext;
    private readonly ILogger<UserProvisioningService> _logger;

    public UserProvisioningService(
        WorkBaseDbContext dbContext,
        ILogger<UserProvisioningService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Fallback for direct/non-Hub accounts that do not carry hub_role. Hub-brokered users
    /// are mapped explicitly, with Super Admin reserved for the operator tenant.
    /// </summary>
    private const string DefaultRoleName = "Admin";

    /// <summary>
    /// Role reserved for the operator company's owner. Customer owners receive Admin.
    /// </summary>
    private const string OwnerRoleName = "Super Admin";
    private const string MemberRoleName = "Pracownik";
    private const string SystemAssignedBy = "system";
    private static readonly string[] HubManagedRoleNames = [OwnerRoleName, DefaultRoleName, MemberRoleName];

    public async Task EnsureUserProvisionedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var keycloakId = principal.FindFirstValue("sub") 
            ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId))
            return;

        var tenantIdClaim = principal.FindFirstValue("tenant_id");
        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("User {KeycloakId} has no valid tenant_id claim; provisioning rejected", keycloakId);
            return;
        }

        var hubRoleName = HubSsoService.MapApplicationRole(
            principal.FindFirstValue("hub_role"), tenantId);
        var fallbackRoleName = principal.IsInRole("workbase-kiosk")
            ? MemberRoleName
            : tenantId == WorkBase.Shared.Auth.PlatformConstants.OperatorTenantId
            ? DefaultRoleName
            : MemberRoleName;
        var targetRoleName = hubRoleName ?? fallbackRoleName;

        var existingUser = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(
                u => u.KeycloakId == keycloakId && u.TenantId == tenantId,
                cancellationToken);

        if (existingUser is not null)
        {
            existingUser.UpdateLastLogin();

            if (hubRoleName is not null)
            {
                // Synchronize only integration-managed assignments. Roles granted manually
                // have AssignedBy=<administrator sub> and must remain untouched.
                var targetRoleId = await GetRoleIdByNameAsync(existingUser.TenantId, hubRoleName, cancellationToken);
                if (targetRoleId is not null)
                {
                    await RemoveOtherCustomerAdminsAsync(
                        existingUser.TenantId,
                        targetRoleId.Value,
                        existingUser.Id,
                        hubRoleName,
                        cancellationToken);

                    var managedAssignments = await _dbContext.Set<UserRole>()
                        .Where(ur => ur.UserId == existingUser.Id &&
                                     ur.TenantId == existingUser.TenantId &&
                                     ur.AssignedBy == SystemAssignedBy)
                        .Join(
                            _dbContext.Set<Role>().Where(r => HubManagedRoleNames.Contains(r.Name)),
                            ur => ur.RoleId,
                            role => role.Id,
                            (ur, role) => new { UserRole = ur, RoleName = role.Name })
                        .ToListAsync(cancellationToken);

                    var obsoleteAssignments = managedAssignments
                        .Where(assignment => assignment.RoleName != hubRoleName)
                        .Select(assignment => assignment.UserRole)
                        .ToList();
                    if (obsoleteAssignments.Count > 0)
                        _dbContext.Set<UserRole>().RemoveRange(obsoleteAssignments);

                    if (managedAssignments.All(assignment => assignment.RoleName != hubRoleName))
                    {
                        _dbContext.Set<UserRole>().Add(
                            UserRole.Create(existingUser.Id, targetRoleId.Value, existingUser.TenantId, SystemAssignedBy));
                    }

                    if (obsoleteAssignments.Count > 0 || managedAssignments.All(assignment => assignment.RoleName != hubRoleName))
                    {
                        _logger.LogInformation(
                            "Synchronized Hub role for user {UserId}: hub_role={HubRole}, WorkBase role={RoleName}",
                            existingUser.Id, principal.FindFirstValue("hub_role"), hubRoleName);
                    }
                }
            }
            else
            {
                // Direct/non-HUB login: only operator accounts retain the historical Admin
                // fallback. Customer users without a HUB-managed role receive Pracownik.
                var hasAnyRole = await _dbContext.Set<UserRole>()
                    .AnyAsync(ur => ur.UserId == existingUser.Id && ur.TenantId == existingUser.TenantId, cancellationToken);

                if (!hasAnyRole)
                {
                    var fallbackRoleId = await GetRoleIdByNameAsync(
                        existingUser.TenantId, fallbackRoleName, cancellationToken);
                    if (fallbackRoleId is not null)
                    {
                        _dbContext.Set<UserRole>().Add(
                            UserRole.Create(existingUser.Id, fallbackRoleId.Value, existingUser.TenantId, SystemAssignedBy));
                        _logger.LogInformation(
                            "Assigned fallback {RoleName} role to direct-login user {UserId}",
                            fallbackRoleName, existingUser.Id);
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var email = principal.FindFirstValue("email") ?? "";
        var firstName = principal.FindFirstValue("given_name") ?? "";
        var lastName = principal.FindFirstValue("family_name") ?? "";

        var user = User.Create(keycloakId, email, firstName, lastName, tenantId);

        _dbContext.Set<User>().Add(user);

        // HUB roles are tenant-aware: only the operator owner may receive Super Admin.
        // Direct/non-HUB accounts retain the historical Admin fallback.
        var defaultRoleId = await GetRoleIdByNameAsync(tenantId, targetRoleName, cancellationToken)
            ?? await GetRoleIdByNameAsync(tenantId, DefaultRoleName, cancellationToken);

        if (defaultRoleId is not null)
        {
            await RemoveOtherCustomerAdminsAsync(
                tenantId,
                defaultRoleId.Value,
                user.Id,
                targetRoleName,
                cancellationToken);

            var userRole = UserRole.Create(user.Id, defaultRoleId.Value, tenantId, SystemAssignedBy);
            _dbContext.Set<UserRole>().Add(userRole);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            // Benign first-login race: OnTokenValidated runs for EVERY authenticated request,
            // and a fresh SPA session fires several in parallel — each sees "no user yet" and
            // tries to insert. One wins, the rest hit the unique index on keycloak_id. The
            // user exists now, which is all we need.
            _logger.LogDebug("User {KeycloakId} was provisioned concurrently by another request, skipping.", keycloakId);
            return;
        }

        _logger.LogInformation(
            "Provisioned new user {UserId} (Keycloak: {KeycloakId}, Tenant: {TenantId}) with role {RoleName}",
            user.Id, keycloakId, tenantId, targetRoleName);
    }

    private async Task<Guid?> GetRoleIdByNameAsync(Guid tenantId, string roleName, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Role>()
            .Where(r => r.TenantId == tenantId && r.Name == roleName)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task RemoveOtherCustomerAdminsAsync(
        Guid tenantId,
        Guid targetRoleId,
        Guid targetUserId,
        string targetRoleName,
        CancellationToken cancellationToken)
    {
        if (tenantId == WorkBase.Shared.Auth.PlatformConstants.OperatorTenantId
            || targetRoleName != DefaultRoleName)
        {
            return;
        }

        var obsoleteAssignments = await _dbContext.Set<UserRole>()
            .Where(assignment => assignment.TenantId == tenantId
                                 && assignment.RoleId == targetRoleId
                                 && assignment.UserId != targetUserId)
            .ToListAsync(cancellationToken);
        if (obsoleteAssignments.Count == 0)
            return;

        _dbContext.Set<UserRole>().RemoveRange(obsoleteAssignments);
        _logger.LogInformation(
            "Transferred customer Admin role to user {UserId} in tenant {TenantId}",
            targetUserId, tenantId);
    }

    public static async Task<bool> OnTokenValidatedAsync(IServiceProvider serviceProvider, ClaimsPrincipal principal)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserProvisioningService>>();
        try
        {
            var provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
            await provisioningService.EnsureUserProvisionedAsync(principal);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User provisioning failed for sub={Sub}", principal.FindFirstValue("sub"));
            return false;
        }
    }
}

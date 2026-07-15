using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// Name of the role assigned by default to newly provisioned users. Resolved per-tenant
    /// (via GetDefaultRoleIdAsync) rather than a single hardcoded RoleId, since Role rows are
    /// tenant-scoped — every tenant seeded via IamSeeder.SeedTenantRbacAsync has its own
    /// "Admin" role with a different Id.
    /// </summary>
    private const string DefaultRoleName = "Admin";

    /// <summary>
    /// Role granted to the licence owner. When a company receives a WorkBase licence via
    /// wb-platform, the Hub embeds hub_role="owner" in the brokered token (see
    /// hub-api oidc/token) for the organization owner — that account is elevated to the
    /// full "Super Admin" role here rather than the default "Admin".
    /// </summary>
    private const string OwnerRoleName = "Super Admin";
    private const string HubRoleOwnerClaim = "owner";

    private static bool IsHubOwner(ClaimsPrincipal principal) =>
        string.Equals(principal.FindFirstValue("hub_role"), HubRoleOwnerClaim, StringComparison.OrdinalIgnoreCase);

    public async Task EnsureUserProvisionedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var keycloakId = principal.FindFirstValue("sub") 
            ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId))
            return;

        var isOwner = IsHubOwner(principal);
        var targetRoleName = isOwner ? OwnerRoleName : DefaultRoleName;

        var existingUser = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.UpdateLastLogin();

            var currentRoleIds = await _dbContext.Set<UserRole>()
                .Where(ur => ur.UserId == existingUser.Id)
                .Select(ur => ur.RoleId)
                .ToListAsync(cancellationToken);

            // Owner licence: ensure the owner always holds the Super Admin role, even if the
            // account was provisioned earlier with only the default Admin role.
            if (isOwner)
            {
                var ownerRoleId = await GetRoleIdByNameAsync(existingUser.TenantId, OwnerRoleName, cancellationToken);
                if (ownerRoleId is not null && !currentRoleIds.Contains(ownerRoleId.Value))
                {
                    _dbContext.Set<UserRole>().Add(
                        UserRole.Create(existingUser.Id, ownerRoleId.Value, existingUser.TenantId, "system"));
                    _logger.LogInformation("Elevated existing user {UserId} to Super Admin (Hub licence owner)", existingUser.Id);
                }
            }
            // Ensure user has at least one role (retroactive fix for users provisioned before role assignment)
            else if (currentRoleIds.Count == 0)
            {
                var existingUserRoleId = await GetRoleIdByNameAsync(existingUser.TenantId, DefaultRoleName, cancellationToken);

                if (existingUserRoleId is not null)
                {
                    var userRole = UserRole.Create(existingUser.Id, existingUserRoleId.Value, existingUser.TenantId, "system");
                    _dbContext.Set<UserRole>().Add(userRole);
                    _logger.LogInformation("Assigned default Admin role to existing user {UserId}", existingUser.Id);
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var email = principal.FindFirstValue("email") ?? "";
        var firstName = principal.FindFirstValue("given_name") ?? "";
        var lastName = principal.FindFirstValue("family_name") ?? "";
        var tenantIdClaim = principal.FindFirstValue("tenant_id");

        Guid tenantId;
        if (!Guid.TryParse(tenantIdClaim, out tenantId))
        {
            // Default tenant for users without tenant_id claim (dev/MVP)
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _logger.LogInformation("User {KeycloakId} has no tenant_id claim, using default tenant {TenantId}", keycloakId, tenantId);
        }

        var user = User.Create(keycloakId, email, firstName, lastName, tenantId);

        _dbContext.Set<User>().Add(user);

        // Assign role based on the Hub role: licence owner → Super Admin, otherwise → Admin.
        var defaultRoleId = await GetRoleIdByNameAsync(tenantId, targetRoleName, cancellationToken)
            ?? await GetRoleIdByNameAsync(tenantId, DefaultRoleName, cancellationToken);

        if (defaultRoleId is not null)
        {
            var userRole = UserRole.Create(user.Id, defaultRoleId.Value, tenantId, "system");
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

    public static async Task OnTokenValidatedAsync(IServiceProvider serviceProvider, ClaimsPrincipal principal)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserProvisioningService>>();
        try
        {
            var provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
            await provisioningService.EnsureUserProvisionedAsync(principal);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User provisioning failed for sub={Sub}", principal.FindFirstValue("sub"));
        }
    }
}

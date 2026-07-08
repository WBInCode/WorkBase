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

    public async Task EnsureUserProvisionedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var keycloakId = principal.FindFirstValue("sub") 
            ?? principal.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId))
            return;

        var existingUser = await _dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.KeycloakId == keycloakId, cancellationToken);

        if (existingUser is not null)
        {
            existingUser.UpdateLastLogin();

            // Ensure user has at least one role (retroactive fix for users provisioned before role assignment)
            var hasAnyRole = await _dbContext.Set<UserRole>()
                .AnyAsync(ur => ur.UserId == existingUser.Id, cancellationToken);

            if (!hasAnyRole)
            {
                var defaultRoleId = await GetDefaultRoleIdAsync(existingUser.TenantId, cancellationToken);

                if (defaultRoleId is not null)
                {
                    var userRole = UserRole.Create(existingUser.Id, defaultRoleId.Value, existingUser.TenantId, "system");
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

        // Assign default Admin role if it exists for this tenant
        var defaultRoleId = await GetDefaultRoleIdAsync(tenantId, cancellationToken);

        if (defaultRoleId is not null)
        {
            var userRole = UserRole.Create(user.Id, defaultRoleId.Value, tenantId, "system");
            _dbContext.Set<UserRole>().Add(userRole);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned new user {UserId} (Keycloak: {KeycloakId}, Tenant: {TenantId}) with default Admin role",
            user.Id, keycloakId, tenantId);
    }

    private async Task<Guid?> GetDefaultRoleIdAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Role>()
            .Where(r => r.TenantId == tenantId && r.Name == DefaultRoleName)
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

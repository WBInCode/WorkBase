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
    /// Admin role ID from IamSeeder — assigned by default to newly provisioned users.
    /// </summary>
    private static readonly Guid DefaultRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public async Task EnsureUserProvisionedAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var keycloakId = principal.FindFirstValue("sub");
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
                var defaultRoleExists = await _dbContext.Set<Role>()
                    .AnyAsync(r => r.Id == DefaultRoleId, cancellationToken);

                if (defaultRoleExists)
                {
                    var userRole = UserRole.Create(existingUser.Id, DefaultRoleId, existingUser.TenantId, "system");
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

        if (!Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            _logger.LogWarning("User {KeycloakId} has no valid tenant_id claim, skipping provisioning", keycloakId);
            return;
        }

        var user = User.Create(keycloakId, email, firstName, lastName, tenantId);

        _dbContext.Set<User>().Add(user);

        // Assign default Admin role if it exists
        var roleExists = await _dbContext.Set<Role>()
            .AnyAsync(r => r.Id == DefaultRoleId, cancellationToken);

        if (roleExists)
        {
            var userRole = UserRole.Create(user.Id, DefaultRoleId, tenantId, "system");
            _dbContext.Set<UserRole>().Add(userRole);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned new user {UserId} (Keycloak: {KeycloakId}, Tenant: {TenantId}) with default Admin role",
            user.Id, keycloakId, tenantId);
    }

    public static async Task OnTokenValidatedAsync(IServiceProvider serviceProvider, ClaimsPrincipal principal)
    {
        using var scope = serviceProvider.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.EnsureUserProvisionedAsync(principal);
    }
}

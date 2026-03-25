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
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned new user {UserId} (Keycloak: {KeycloakId}, Tenant: {TenantId})",
            user.Id, keycloakId, tenantId);
    }

    public static async Task OnTokenValidatedAsync(IServiceProvider serviceProvider, ClaimsPrincipal principal)
    {
        using var scope = serviceProvider.CreateScope();
        var provisioningService = scope.ServiceProvider.GetRequiredService<UserProvisioningService>();
        await provisioningService.EnsureUserProvisionedAsync(principal);
    }
}

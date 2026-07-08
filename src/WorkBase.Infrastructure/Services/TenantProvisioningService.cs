using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Seeding;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Services;

public sealed class TenantProvisioningService(
    WorkBaseDbContext dbContext,
    ILogger<TenantProvisioningService> logger) : ITenantProvisioningService
{
    public async Task<Guid> CreateTenantAsync(string name, string slug, CancellationToken cancellationToken = default)
    {
        var tenant = Tenant.Create(name, slug);
        dbContext.Set<Tenant>().Add(tenant);

        // Tenant.Id is database/convention-generated (UUID v7) on insert — populated on the
        // tracked entity after SaveChangesAsync, so it's safe to read tenant.Id below.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Translated to a plain BCL exception here (Infrastructure layer, where EF Core
            // types belong) so callers in other layers (e.g. Organization.Api, which doesn't
            // reference EF Core) can catch it without a new package dependency.
            throw new InvalidOperationException($"Tenant with slug '{slug}' already exists.", ex);
        }

        logger.LogInformation("Created tenant {TenantId} ({Name}/{Slug})", tenant.Id, name, slug);

        // A brand-new tenant has no Roles/Permissions/DataScopes of its own yet — without
        // this, its first provisioned user would get zero working permissions (see
        // UserProvisioningService.GetDefaultRoleIdAsync).
        await IamSeeder.SeedTenantRbacAsync(dbContext, tenant.Id, logger);

        return tenant.Id;
    }
}

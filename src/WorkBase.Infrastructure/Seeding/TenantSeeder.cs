using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

/// <summary>
/// Seeds the root <see cref="Tenant"/> row for the default/development tenant.
/// Must run BEFORE IamSeeder/OrganizationSeeder — they assign this same
/// DefaultTenantId to seeded data but never created the Tenant record itself.
/// See docs/05-module-licensing-architecture.md.
/// </summary>
public static class TenantSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<Tenant>().AnyAsync())
        {
            logger.LogInformation("Tenant data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding default tenant...");

        var tenant = Tenant.Create("WorkBase Development", "workbase-dev");
        SetId(tenant, DefaultTenantId);

        dbContext.Set<Tenant>().Add(tenant);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Default tenant seeded: {TenantId}", DefaultTenantId);
    }

    /// <summary>
    /// Sets the Id on an entity using reflection (entities use private setters).
    /// </summary>
    private static void SetId(Tenant entity, Guid id)
    {
        var prop = typeof(Tenant).GetProperty(nameof(Tenant.Id));
        prop!.SetValue(entity, id);
    }
}

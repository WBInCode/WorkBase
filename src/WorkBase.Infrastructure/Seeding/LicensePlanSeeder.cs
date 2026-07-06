using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Infrastructure.Persistence.Entities;

namespace WorkBase.Infrastructure.Seeding;

/// <summary>
/// Seeds the default starter bundles (Bronze/Silver/Gold — working names, see
/// docs/05-module-licensing-architecture.md §3.4) that operators can assign to a tenant
/// as a quick starting point. Module keys must match ModuleCatalog.All entries.
/// </summary>
public static class LicensePlanSeeder
{
    private static readonly Guid BronzeId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid SilverId = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private static readonly Guid GoldId = Guid.Parse("60000000-0000-0000-0000-000000000003");

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<LicensePlan>().AnyAsync())
        {
            logger.LogInformation("License plans already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding default license plans...");

        var now = DateTime.UtcNow;

        var bronze = new LicensePlan
        {
            Id = BronzeId,
            Name = "Bronze",
            IncludedModules = ["org", "identity", "time", "leave", "tasks", "dashboard", "notification"],
            IsActive = true,
            CreatedAt = now,
        };

        var silver = new LicensePlan
        {
            Id = SilverId,
            Name = "Silver",
            IncludedModules = [.. bronze.IncludedModules, "workflow", "documents", "forms"],
            IsActive = true,
            CreatedAt = now,
        };

        var gold = new LicensePlan
        {
            Id = GoldId,
            Name = "Gold",
            IncludedModules = [.. silver.IncludedModules, "integration", "cases", "contacts", "sales", "ai"],
            IsActive = true,
            CreatedAt = now,
        };

        dbContext.Set<LicensePlan>().AddRange(bronze, silver, gold);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} license plans.", 3);
    }
}

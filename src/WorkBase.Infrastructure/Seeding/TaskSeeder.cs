using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Domain.Entities;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Infrastructure.Seeding;

public static class TaskSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Statusy i priorytety zadan dla wskazanej firmy. Dopisuje tylko brakujace kody, wiec
    /// mozna wolac wielokrotnie — provisioning powtarza sie przy kazdej synchronizacji z Hubem.
    /// </summary>
    /// <remarks>
    /// <see cref="SeedAsync"/> obsluguje wylacznie firme operatora i przerywa, gdy istnieje
    /// JAKIKOLWIEK status w bazie. Bez wariantu per-firma nowa firma nie mogla zalozyc zadania:
    /// TaskItem wymaga identyfikatora statusu, a statusow nie bylo.
    /// </remarks>
    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // IgnoreQueryFilters, bo to leci poza zadaniem HTTP — globalny filtr najemcy
        // nie ma wtedy z czego odczytac firmy.
        var kodyStatusow = await dbContext.Set<TaskStatus>()
            .IgnoreQueryFilters()
            .Where(status => status.TenantId == tenantId)
            .Select(status => status.Code)
            .ToListAsync(cancellationToken);

        var kodyPriorytetow = await dbContext.Set<TaskPriority>()
            .IgnoreQueryFilters()
            .Where(priorytet => priorytet.TenantId == tenantId)
            .Select(priorytet => priorytet.Code)
            .ToListAsync(cancellationToken);

        var brakujaceStatusy = DomyslneStatusy(tenantId)
            .Where(status => !kodyStatusow.Contains(status.Code))
            .ToList();

        var brakujacePriorytety = DomyslnePriorytety(tenantId)
            .Where(priorytet => !kodyPriorytetow.Contains(priorytet.Code))
            .ToList();

        if (brakujaceStatusy.Count == 0 && brakujacePriorytety.Count == 0) return;

        dbContext.Set<TaskStatus>().AddRange(brakujaceStatusy);
        dbContext.Set<TaskPriority>().AddRange(brakujacePriorytety);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Dopisano {Statusy} statusow i {Priorytety} priorytetow zadan dla firmy {TenantId}.",
            brakujaceStatusy.Count, brakujacePriorytety.Count, tenantId);
    }

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<TaskStatus>().AnyAsync())
        {
            logger.LogInformation("Task statuses already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding task statuses and priorities...");

        var statuses = DomyslneStatusy(DefaultTenantId);
        dbContext.Set<TaskStatus>().AddRange(statuses);

        var priorities = DomyslnePriorytety(DefaultTenantId);
        dbContext.Set<TaskPriority>().AddRange(priorities);

        await dbContext.SaveChangesAsync();

        logger.LogInformation("Task seeding completed: {StatusCount} statuses, {PriorityCount} priorities.",
            statuses.Count, priorities.Count);
    }

    private static List<TaskStatus> DomyslneStatusy(Guid tenantId)
    {
        return new List<TaskStatus>
        {
            TaskStatus.Create(tenantId, "NEW", "Nowe",
                isFinal: false, isDefault: true, color: "#2196F3", sortOrder: 1),
            TaskStatus.Create(tenantId, "IN_PROGRESS", "W toku",
                isFinal: false, isDefault: false, color: "#FF9800", sortOrder: 2),
            TaskStatus.Create(tenantId, "REVIEW", "Do akceptacji",
                isFinal: false, isDefault: false, color: "#9C27B0", sortOrder: 3),
            TaskStatus.Create(tenantId, "CLOSED", "Zamknięte",
                isFinal: true, isDefault: false, color: "#4CAF50", sortOrder: 4),
        };
    }

    private static List<TaskPriority> DomyslnePriorytety(Guid tenantId)
    {
        return new List<TaskPriority>
        {
            TaskPriority.Create(tenantId, "LOW", "Niski", color: "#8BC34A", sortOrder: 1),
            TaskPriority.Create(tenantId, "NORMAL", "Normalny", color: "#2196F3", sortOrder: 2),
            TaskPriority.Create(tenantId, "HIGH", "Wysoki", color: "#FF9800", sortOrder: 3),
            TaskPriority.Create(tenantId, "CRITICAL", "Krytyczny", color: "#F44336", sortOrder: 4),
        };
    }
}

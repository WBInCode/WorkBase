using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Domain.Entities;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Infrastructure.Seeding;

public static class TaskSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<TaskStatus>().AnyAsync())
        {
            logger.LogInformation("Task statuses already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding task statuses and priorities...");

        var statuses = new List<TaskStatus>
        {
            TaskStatus.Create(DefaultTenantId, "NEW", "Nowe",
                isFinal: false, isDefault: true, color: "#2196F3", sortOrder: 1),
            TaskStatus.Create(DefaultTenantId, "IN_PROGRESS", "W toku",
                isFinal: false, isDefault: false, color: "#FF9800", sortOrder: 2),
            TaskStatus.Create(DefaultTenantId, "REVIEW", "Do akceptacji",
                isFinal: false, isDefault: false, color: "#9C27B0", sortOrder: 3),
            TaskStatus.Create(DefaultTenantId, "CLOSED", "Zamknięte",
                isFinal: true, isDefault: false, color: "#4CAF50", sortOrder: 4),
        };

        dbContext.Set<TaskStatus>().AddRange(statuses);

        var priorities = new List<TaskPriority>
        {
            TaskPriority.Create(DefaultTenantId, "LOW", "Niski", color: "#8BC34A", sortOrder: 1),
            TaskPriority.Create(DefaultTenantId, "NORMAL", "Normalny", color: "#2196F3", sortOrder: 2),
            TaskPriority.Create(DefaultTenantId, "HIGH", "Wysoki", color: "#FF9800", sortOrder: 3),
            TaskPriority.Create(DefaultTenantId, "CRITICAL", "Krytyczny", color: "#F44336", sortOrder: 4),
        };

        dbContext.Set<TaskPriority>().AddRange(priorities);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Task seeding completed: {StatusCount} statuses, {PriorityCount} priorities.",
            statuses.Count, priorities.Count);
    }
}

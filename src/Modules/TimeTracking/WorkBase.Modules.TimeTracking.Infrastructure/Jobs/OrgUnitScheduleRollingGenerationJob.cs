using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Infrastructure.Jobs;

public sealed class OrgUnitScheduleRollingGenerationJob(
    WorkBaseDbContext dbContext,
    OrgUnitScheduleGeneratorService generatorService,
    ILogger<OrgUnitScheduleRollingGenerationJob> logger)
{
    public async Task ExecuteAsync()
    {
        logger.LogInformation("Starting rolling org-unit schedule generation");

        var activeSchedules = await dbContext.Set<OrgUnitSchedule>()
            .Where(s => s.IsActive)
            .ToListAsync();

        if (activeSchedules.Count == 0)
        {
            logger.LogInformation("No active org-unit schedules found, skipping");
            return;
        }

        var from = DateOnly.FromDateTime(DateTime.UtcNow);
        var to = from.AddDays(28); // 4 weeks ahead

        var generated = 0;
        foreach (var schedule in activeSchedules)
        {
            try
            {
                await generatorService.GenerateForOrgUnitAsync(
                    schedule.TenantId,
                    schedule.Id,
                    from,
                    to);
                generated++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to generate schedules for OrgUnitSchedule {Id} (OrgUnit {OrgUnitId})",
                    schedule.Id, schedule.OrgUnitId);
            }
        }

        logger.LogInformation(
            "Rolling generation complete: processed {Count}/{Total} org-unit schedules for {From} to {To}",
            generated, activeSchedules.Count, from, to);
    }
}

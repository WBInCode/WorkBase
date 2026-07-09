using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Events;
using WorkBase.Shared.Domain;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Infrastructure.Jobs;

public sealed class TaskOverdueDetectorJob(
    WorkBaseDbContext dbContext,
    IPublisher publisher,
    ITenantConfigService tenantConfig,
    ILogger<TaskOverdueDetectorJob> logger)
{
    private const string SettingsKey = "task_overdue";

    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        logger.LogInformation("Starting overdue task detection at {Timestamp}", now);

        var finalStatusIds = await dbContext.Set<TaskStatus>()
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync();

        // Candidates: anything past its raw due date. Per-tenant grace period (applied below)
        // may still exclude some of these from actually being published as overdue.
        var candidateTasks = await dbContext.Set<TaskItem>()
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value < now
                && t.CompletedAt == null
                && !finalStatusIds.Contains(t.StatusId))
            .Select(t => new { t.Id, t.TenantId, t.AssigneeId, t.Title, DueDate = t.DueDate!.Value })
            .ToListAsync();

        var settingsCache = new Dictionary<Guid, TaskOverdueSettings>();
        var publishedCount = 0;

        foreach (var task in candidateTasks)
        {
            if (!settingsCache.TryGetValue(task.TenantId, out var settings))
            {
                settings = await tenantConfig.GetAsync<TaskOverdueSettings>(task.TenantId, SettingsKey)
                    ?? new TaskOverdueSettings();
                settingsCache[task.TenantId] = settings;
            }

            if (!settings.NotifyOnOverdue) continue;
            if (now < task.DueDate.AddHours(settings.GracePeriodHours)) continue;

            try
            {
                await publisher.Publish(new TaskOverdueEvent(
                    task.Id, task.TenantId, task.AssigneeId,
                    task.Title, task.DueDate));
                publishedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error publishing overdue event for task {TaskId} in tenant {TenantId}",
                    task.Id, task.TenantId);
            }
        }

        logger.LogInformation(
            "Overdue task detection completed. {Candidates} candidates, published {Count} overdue events.",
            candidateTasks.Count, publishedCount);
    }
}


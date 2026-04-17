using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Events;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Modules.Tasks.Infrastructure.Jobs;

public sealed class TaskOverdueDetectorJob(
    WorkBaseDbContext dbContext,
    IPublisher publisher,
    ILogger<TaskOverdueDetectorJob> logger)
{
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        logger.LogInformation("Starting overdue task detection at {Timestamp}", now);

        var finalStatusIds = await dbContext.Set<TaskStatus>()
            .Where(s => s.IsFinal)
            .Select(s => s.Id)
            .ToListAsync();

        var overdueTasks = await dbContext.Set<TaskItem>()
            .Where(t => t.DueDate.HasValue
                && t.DueDate.Value < now
                && t.CompletedAt == null
                && !finalStatusIds.Contains(t.StatusId))
            .Select(t => new { t.Id, t.TenantId, t.AssigneeId, t.Title, DueDate = t.DueDate!.Value })
            .ToListAsync();

        logger.LogInformation("Found {Count} overdue tasks", overdueTasks.Count);

        foreach (var task in overdueTasks)
        {
            try
            {
                await publisher.Publish(new TaskOverdueEvent(
                    task.Id, task.TenantId, task.AssigneeId,
                    task.Title, task.DueDate));
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error publishing overdue event for task {TaskId} in tenant {TenantId}",
                    task.Id, task.TenantId);
            }
        }

        logger.LogInformation(
            "Overdue task detection completed. Published {Count} overdue events.",
            overdueTasks.Count);
    }
}

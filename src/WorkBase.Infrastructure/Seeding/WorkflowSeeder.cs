using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Infrastructure.Seeding;

public static class WorkflowSeeder
{
    private static readonly Guid DefaultTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(WorkBaseDbContext dbContext, ILogger logger)
    {
        if (await dbContext.Set<WorkflowDefinition>().AnyAsync())
        {
            logger.LogInformation("Workflow definitions already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding workflow definitions...");

        var definitions = new List<WorkflowDefinition>
        {
            CreateLeaveRequestDefinition(DefaultTenantId),
            CreateTaskAcceptanceDefinition(DefaultTenantId),
        };

        dbContext.Set<WorkflowDefinition>().AddRange(definitions);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Workflow seeding completed: {Count} definitions.", definitions.Count);
    }

    /// <summary>
    /// Definicje obiegów dla wskazanego najemcy. <see cref="SeedAsync"/> obsługuje wyłącznie
    /// najemcę domyślnego, więc bez tego każda nowa firma zostaje bez obiegu akceptacji.
    /// </summary>
    public static async Task SeedTenantAsync(
        WorkBaseDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var istniejace = await dbContext.Set<WorkflowDefinition>()
            .IgnoreQueryFilters()
            .Where(d => d.TenantId == tenantId)
            .Select(d => d.Name)
            .ToListAsync(cancellationToken);

        foreach (var definicja in new[]
                 {
                     CreateLeaveRequestDefinition(tenantId),
                     CreateTaskAcceptanceDefinition(tenantId),
                 })
        {
            if (istniejace.Contains(definicja.Name)) continue;
            dbContext.Set<WorkflowDefinition>().Add(definicja);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static WorkflowDefinition CreateLeaveRequestDefinition(Guid tenantId)
    {
        const string json = """
        {
            "name": "leave-request-v1",
            "version": 1,
            "entityType": "LeaveRequest",
            "initialStep": "Draft",
            "steps": [
                {
                    "name": "Draft",
                    "type": "action",
                    "transitions": [
                        { "outcome": "submitted", "targetStep": "SupervisorApproval" }
                    ],
                    "actions": [
                        { "type": "notify", "trigger": "on_enter", "payload": { "template": "leave_request_created" } }
                    ]
                },
                {
                    "name": "SupervisorApproval",
                    "type": "approval",
                    "approverStrategy": "supervisor",
                    "transitions": [
                        { "outcome": "approved", "targetStep": "Approved" },
                        { "outcome": "rejected", "targetStep": "Rejected" },
                        { "outcome": "returned", "targetStep": "Draft" }
                    ],
                    "actions": [
                        { "type": "notify", "trigger": "on_enter", "payload": { "template": "leave_approval_pending", "target": "approver" } },
                        { "type": "notify", "trigger": "on_complete", "payload": { "template": "leave_decision_made", "target": "requester" } }
                    ]
                },
                {
                    "name": "Approved",
                    "type": "end"
                },
                {
                    "name": "Rejected",
                    "type": "end"
                }
            ]
        }
        """;

        return WorkflowDefinition.Create(
            tenantId,
            "leave-request-v1",
            json,
            "Wniosek urlopowy: Draft → SupervisorApproval → Approved/Rejected");
    }

    private static WorkflowDefinition CreateTaskAcceptanceDefinition(Guid tenantId)
    {
        const string json = """
        {
            "name": "task-acceptance-v1",
            "version": 1,
            "entityType": "TaskAssignment",
            "initialStep": "Pending",
            "steps": [
                {
                    "name": "Pending",
                    "type": "action",
                    "transitions": [
                        { "outcome": "accepted", "targetStep": "Accepted" },
                        { "outcome": "returned", "targetStep": "Returned" }
                    ],
                    "actions": [
                        { "type": "notify", "trigger": "on_enter", "payload": { "template": "task_assigned", "target": "assignee" } }
                    ]
                },
                {
                    "name": "Returned",
                    "type": "action",
                    "transitions": [
                        { "outcome": "reassigned", "targetStep": "Pending" },
                        { "outcome": "cancelled", "targetStep": "Cancelled" }
                    ],
                    "actions": [
                        { "type": "notify", "trigger": "on_enter", "payload": { "template": "task_returned", "target": "assigner" } }
                    ]
                },
                {
                    "name": "Accepted",
                    "type": "end"
                },
                {
                    "name": "Cancelled",
                    "type": "end"
                }
            ]
        }
        """;

        return WorkflowDefinition.Create(
            tenantId,
            "task-acceptance-v1",
            json,
            "Akceptacja zadania: Pending → Accepted/Returned → Reassigned/Cancelled");
    }
}

using Microsoft.Extensions.Logging;
using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Infrastructure;

/// <summary>
/// Dispatches workflow actions by type (notify, create_task, update_entity).
/// Each action is marked as Success or Failed after execution.
/// </summary>
public sealed class WorkflowActionExecutor(ILogger<WorkflowActionExecutor> logger) : IWorkflowActionExecutor
{
    public async Task ExecuteAsync(WorkflowAction action, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (action.ActionType)
            {
                case "notify":
                    await ExecuteNotifyAsync(action, cancellationToken);
                    break;

                case "create_task":
                    await ExecuteCreateTaskAsync(action, cancellationToken);
                    break;

                case "update_entity":
                    await ExecuteUpdateEntityAsync(action, cancellationToken);
                    break;

                default:
                    logger.LogWarning(
                        "Unknown workflow action type '{ActionType}' for action {ActionId}",
                        action.ActionType, action.Id);
                    action.MarkFailed($"Unknown action type: {action.ActionType}");
                    return;
            }

            action.MarkSuccess();

            logger.LogInformation(
                "Workflow action {ActionId} ({ActionType}) executed successfully for instance {InstanceId}",
                action.Id, action.ActionType, action.InstanceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Workflow action {ActionId} ({ActionType}) failed for instance {InstanceId}",
                action.Id, action.ActionType, action.InstanceId);
            action.MarkFailed(ex.Message);
        }
    }

    private Task ExecuteNotifyAsync(WorkflowAction action, CancellationToken cancellationToken)
    {
        // Log the notification intent. When Notification module (SignalR/email) is ready,
        // this will dispatch through INotificationService.
        logger.LogInformation(
            "Notify action for instance {InstanceId}, step {StepId}: {Payload}",
            action.InstanceId, action.StepId, action.PayloadJson ?? "(no payload)");

        return Task.CompletedTask;
    }

    private Task ExecuteCreateTaskAsync(WorkflowAction action, CancellationToken cancellationToken)
    {
        // Log the create_task intent. When Tasks cross-module contract is ready,
        // this will create a task via ITaskService.
        logger.LogInformation(
            "CreateTask action for instance {InstanceId}, step {StepId}: {Payload}",
            action.InstanceId, action.StepId, action.PayloadJson ?? "(no payload)");

        return Task.CompletedTask;
    }

    private Task ExecuteUpdateEntityAsync(WorkflowAction action, CancellationToken cancellationToken)
    {
        // Log the update_entity intent. When cross-module entity update contract is ready,
        // this will update the entity through the owning module.
        logger.LogInformation(
            "UpdateEntity action for instance {InstanceId}, step {StepId}: {Payload}",
            action.InstanceId, action.StepId, action.PayloadJson ?? "(no payload)");

        return Task.CompletedTask;
    }
}

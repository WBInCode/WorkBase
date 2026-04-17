using WorkBase.Modules.Workflow.Domain.Entities;

namespace WorkBase.Modules.Workflow.Application.Contracts;

/// <summary>
/// Executes workflow actions (notify, create_task, update_entity) as side effects.
/// </summary>
public interface IWorkflowActionExecutor
{
    /// <summary>
    /// Executes a workflow action and marks it as Success or Failed.
    /// </summary>
    Task ExecuteAsync(WorkflowAction action, CancellationToken cancellationToken = default);
}

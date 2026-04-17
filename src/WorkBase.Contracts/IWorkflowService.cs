namespace WorkBase.Contracts;

/// <summary>
/// Cross-module contract for creating and managing workflow instances.
/// Implemented by Workflow module, consumed by Leave (and other) modules.
/// </summary>
public interface IWorkflowService
{
    /// <summary>
    /// Creates a new workflow instance from a named definition.
    /// Returns the workflow instance ID, or null if the definition was not found.
    /// </summary>
    Task<Guid?> CreateInstanceAsync(
        Guid tenantId,
        string definitionName,
        string entityType,
        Guid entityId,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default);
}

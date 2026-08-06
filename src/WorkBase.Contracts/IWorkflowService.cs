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
    /// <param name="initialOutcome">
    /// Rozstrzygnięcie, którym obieg ma od razu opuścić krok startowy (np. "submitted").
    /// Bez niego obieg zatrzyma się przed krokiem akceptacji.
    /// </param>
    Task<Guid?> CreateInstanceAsync(
        Guid tenantId,
        string definitionName,
        string entityType,
        Guid entityId,
        Guid initiatedByUserId,
        string? initialOutcome = null,
        CancellationToken cancellationToken = default);
}

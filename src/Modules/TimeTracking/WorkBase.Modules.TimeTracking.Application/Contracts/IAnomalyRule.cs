using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

/// <summary>
/// A single anomaly detection rule. Implementations analyze time data for a given
/// employee/date and return any detected anomalies.
/// </summary>
public interface IAnomalyRule
{
    /// <summary>
    /// Evaluates the rule against the provided context and returns detected anomalies (if any).
    /// </summary>
    Task<List<TimeAnomaly>> EvaluateAsync(AnomalyRuleContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Contextual data passed to each anomaly rule for evaluation.
/// </summary>
public sealed class AnomalyRuleContext
{
    public required Guid TenantId { get; init; }
    public required Guid EmployeeId { get; init; }
    public required DateOnly Date { get; init; }
    public required List<TimeEntry> Entries { get; init; }
    public required Schedule? Schedule { get; init; }
    public required TimeSheet? TimeSheet { get; init; }
    public required AnomalyDetectionSettings Settings { get; init; }

    /// <summary>
    /// Checks if an anomaly of the given type was already detected for this employee/date.
    /// </summary>
    public required Func<AnomalyType, CancellationToken, Task<bool>> AlreadyDetected { get; init; }
}

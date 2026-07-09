namespace WorkBase.Modules.Tasks.Application.Contracts;

/// <summary>
/// Tenant-configurable behavior for overdue-task detection (docs/AUDIT-KNOWLEDGE-MAP.md —
/// module parametrization). Read by TaskOverdueDetectorJob via ITenantConfigService under
/// the "task_overdue" key.
/// </summary>
public sealed class TaskOverdueSettings
{
    /// <summary>Hours of leeway after the due date before a task is considered overdue.</summary>
    public int GracePeriodHours { get; set; } = 0;

    /// <summary>When false, the tenant opts out of overdue detection entirely.</summary>
    public bool NotifyOnOverdue { get; set; } = true;
}

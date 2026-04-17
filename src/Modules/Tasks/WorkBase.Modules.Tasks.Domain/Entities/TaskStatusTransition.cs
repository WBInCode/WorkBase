using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskStatusTransition : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid FromStatusId { get; private set; }
    public Guid ToStatusId { get; private set; }
    public bool IsActive { get; private set; }

    private TaskStatusTransition() { }

    public static TaskStatusTransition Create(
        Guid tenantId, Guid fromStatusId, Guid toStatusId)
    {
        return new TaskStatusTransition
        {
            TenantId = tenantId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
            IsActive = true,
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}

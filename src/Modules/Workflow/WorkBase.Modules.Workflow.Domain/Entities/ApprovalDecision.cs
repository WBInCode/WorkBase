using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Domain.Entities;

public sealed class ApprovalDecision : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid RequestId { get; private set; }
    public Guid DecidedBy { get; private set; }
    public string Decision { get; private set; } = null!;
    public string? Comment { get; private set; }
    public DateTime DecidedAt { get; private set; }

    private ApprovalDecision() { }

    public static ApprovalDecision Create(
        Guid tenantId,
        Guid requestId,
        Guid decidedBy,
        string decision,
        string? comment = null)
    {
        return new ApprovalDecision
        {
            TenantId = tenantId,
            RequestId = requestId,
            DecidedBy = decidedBy,
            Decision = decision,
            Comment = comment,
            DecidedAt = DateTime.UtcNow,
        };
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Domain.Entities;

/// <summary>
/// Decyzja akceptanta dot. wniosku urlopowego.
/// </summary>
public sealed class LeaveDecision : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public Guid DecidedByEmployeeId { get; private set; }
    public string Decision { get; private set; } = null!;
    public string? Comment { get; private set; }
    public DateTime DecidedAt { get; private set; }

    private LeaveDecision() { }

    public static LeaveDecision Create(
        Guid tenantId,
        Guid leaveRequestId,
        Guid decidedByEmployeeId,
        string decision,
        string? comment = null)
    {
        return new LeaveDecision
        {
            TenantId = tenantId,
            LeaveRequestId = leaveRequestId,
            DecidedByEmployeeId = decidedByEmployeeId,
            Decision = decision,
            Comment = comment,
            DecidedAt = DateTime.UtcNow,
        };
    }
}

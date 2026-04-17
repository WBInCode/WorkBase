using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed record SubmitApprovalDecisionCommand(
    Guid ApprovalRequestId,
    string Decision,
    Guid DecidedByEmployeeId,
    string? Comment = null) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

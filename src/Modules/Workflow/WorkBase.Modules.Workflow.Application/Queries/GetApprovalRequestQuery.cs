using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetApprovalRequestQuery(Guid ApprovalRequestId) : IQuery<ApprovalRequestDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

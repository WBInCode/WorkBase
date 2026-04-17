using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed record GetPendingApprovalsQuery(Guid ApproverEmployeeId) : IQuery<List<ApprovalRequestDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

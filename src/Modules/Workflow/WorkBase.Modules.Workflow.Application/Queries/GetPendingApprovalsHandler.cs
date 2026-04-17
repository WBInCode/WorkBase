using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed class GetPendingApprovalsHandler(
    IApprovalRequestRepository approvalRequestRepository,
    IWorkflowInstanceRepository instanceRepository)
    : IQueryHandler<GetPendingApprovalsQuery, List<ApprovalRequestDto>>
{
    public async Task<Result<List<ApprovalRequestDto>>> Handle(
        GetPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var requests = await approvalRequestRepository.GetPendingByApproverAsync(
            request.TenantId, request.ApproverEmployeeId, cancellationToken);

        var dtos = new List<ApprovalRequestDto>();
        foreach (var r in requests)
        {
            var instance = await instanceRepository.GetByIdAsync(r.InstanceId, cancellationToken);
            dtos.Add(new ApprovalRequestDto(
                r.Id,
                r.InstanceId,
                r.StepId,
                r.RequesterId,
                r.ApproverId,
                r.Status,
                r.DueDate,
                r.Order,
                r.CreatedAt,
                instance?.EntityType,
                instance?.EntityId));
        }

        return dtos;
    }
}

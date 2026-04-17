using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Queries;

public sealed class GetApprovalRequestHandler(
    IApprovalRequestRepository approvalRequestRepository,
    IWorkflowInstanceRepository instanceRepository)
    : IQueryHandler<GetApprovalRequestQuery, ApprovalRequestDto>
{
    public async Task<Result<ApprovalRequestDto>> Handle(
        GetApprovalRequestQuery request, CancellationToken cancellationToken)
    {
        var approvalRequest = await approvalRequestRepository.GetByIdAsync(request.ApprovalRequestId, cancellationToken);
        if (approvalRequest is null)
            return Result.Failure<ApprovalRequestDto>(Error.NotFound("Approval.RequestNotFound",
                $"Wniosek o akceptację o id '{request.ApprovalRequestId}' nie został znaleziony."));

        var instance = await instanceRepository.GetByIdAsync(approvalRequest.InstanceId, cancellationToken);

        return new ApprovalRequestDto(
            approvalRequest.Id,
            approvalRequest.InstanceId,
            approvalRequest.StepId,
            approvalRequest.RequesterId,
            approvalRequest.ApproverId,
            approvalRequest.Status,
            approvalRequest.DueDate,
            approvalRequest.Order,
            approvalRequest.CreatedAt,
            instance?.EntityType,
            instance?.EntityId);
    }
}

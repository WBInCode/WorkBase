using WorkBase.Modules.Workflow.Application.Contracts;
using WorkBase.Modules.Workflow.Domain.Entities;
using WorkBase.Modules.Workflow.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Workflow.Application.Commands;

public sealed class SubmitApprovalDecisionHandler(
    IApprovalRequestRepository approvalRequestRepository,
    IApprovalDecisionRepository approvalDecisionRepository,
    IWorkflowEngine workflowEngine)
    : ICommandHandler<SubmitApprovalDecisionCommand>
{
    private static readonly HashSet<string> ValidDecisions = ["approve", "reject", "return"];

    public async Task<Result> Handle(SubmitApprovalDecisionCommand request, CancellationToken cancellationToken)
    {
        if (!ValidDecisions.Contains(request.Decision))
            return Result.Failure(new Error("Approval.InvalidDecision",
                $"Nieprawidłowa decyzja: '{request.Decision}'. Dozwolone: approve, reject, return."));

        var approvalRequest = await approvalRequestRepository.GetByIdAsync(request.ApprovalRequestId, cancellationToken);
        if (approvalRequest is null)
            return Result.Failure(Error.NotFound("Approval.RequestNotFound",
                $"Wniosek o akceptację o id '{request.ApprovalRequestId}' nie został znaleziony."));

        if (approvalRequest.Status != "Pending")
            return Result.Failure(new Error("Approval.AlreadyDecided",
                $"Wniosek o akceptację został już rozpatrzony (status: {approvalRequest.Status})."));

        if (approvalRequest.ApproverId != request.DecidedByEmployeeId)
            return Result.Failure(Error.Forbidden("Approval.NotAuthorized",
                "Tylko przypisany akceptant może podjąć decyzję o tym wniosku."));

        // Create decision record
        var decision = ApprovalDecision.Create(
            request.TenantId,
            request.ApprovalRequestId,
            request.DecidedByEmployeeId,
            request.Decision,
            request.Comment);

        decision.RaiseDomainEvent(new ApprovalDecisionMadeEvent(
            decision.Id,
            request.ApprovalRequestId,
            request.TenantId,
            approvalRequest.InstanceId,
            request.DecidedByEmployeeId,
            request.Decision));

        await approvalDecisionRepository.AddAsync(decision, cancellationToken);

        // Update approval request status
        switch (request.Decision)
        {
            case "approve":
                approvalRequest.Approve();
                break;
            case "reject":
                approvalRequest.Reject();
                break;
            case "return":
                approvalRequest.Return();
                break;
        }

        approvalRequestRepository.Update(approvalRequest);

        // Map decision to workflow outcome and advance the workflow
        var workflowOutcome = request.Decision switch
        {
            "approve" => "approved",
            "reject" => "rejected",
            "return" => "returned",
            _ => request.Decision
        };

        var advanceResult = await workflowEngine.AdvanceStepAsync(
            approvalRequest.InstanceId,
            workflowOutcome,
            request.DecidedByEmployeeId.ToString(),
            request.Comment,
            cancellationToken);

        if (advanceResult.IsFailure)
            return Result.Failure(advanceResult.Error);

        return Result.Success();
    }
}

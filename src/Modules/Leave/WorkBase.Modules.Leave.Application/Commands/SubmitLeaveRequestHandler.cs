using WorkBase.Contracts;
using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Services;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed class SubmitLeaveRequestHandler(
    ILeaveRequestRepository leaveRequestRepository,
    ILeaveBalanceRepository leaveBalanceRepository,
    ILeaveTypeRepository leaveTypeRepository,
    ILeaveBalanceCalculator balanceCalculator,
    IWorkflowService workflowService)
    : ICommandHandler<SubmitLeaveRequestCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // Validate leave type exists and is active
        var leaveType = await leaveTypeRepository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (leaveType is null)
            return Result.Failure<Guid>(Error.NotFound("Leave.TypeNotFound",
                $"Typ nieobecności o id '{request.LeaveTypeId}' nie został znaleziony."));

        if (!leaveType.IsActive)
            return Result.Failure<Guid>(new Error("Leave.TypeInactive",
                $"Typ nieobecności '{leaveType.Name}' jest nieaktywny."));

        // Validate no overlapping requests
        var hasOverlap = await leaveRequestRepository.HasOverlappingRequestAsync(
            request.TenantId, request.EmployeeId, request.StartDate, request.EndDate,
            cancellationToken: cancellationToken);

        if (hasOverlap)
            return Result.Failure<Guid>(Error.Conflict("Leave.DateConflict",
                $"Istnieje już wniosek urlopowy w podanym okresie ({request.StartDate:d} – {request.EndDate:d})."));

        // Validate balance (only for types with limits)
        if (leaveType.DefaultDaysPerYear.HasValue)
        {
            var year = request.StartDate.Year;
            var balance = await leaveBalanceRepository.GetAsync(
                request.TenantId, request.EmployeeId, request.LeaveTypeId, year, cancellationToken);

            if (balance is null)
                return Result.Failure<Guid>(new Error("Leave.NoBalance",
                    $"Brak naliczonego salda urlopowego dla typu '{leaveType.Name}' na rok {year}."));

            var validationResult = balanceCalculator.ValidateBalance(balance, request.TotalDays);
            if (validationResult.IsFailure)
                return Result.Failure<Guid>(validationResult.Error);

            // Reserve days as pending
            balance.AddPending(request.TotalDays);
            leaveBalanceRepository.Update(balance);
        }

        // Create and submit the leave request
        var leaveRequest = LeaveRequest.Create(
            request.TenantId,
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.TotalDays,
            request.Reason);

        if (leaveType.RequiresApproval)
        {
            leaveRequest.Submit();

            // Create workflow instance for approval
            var workflowInstanceId = await workflowService.CreateInstanceAsync(
                request.TenantId,
                "leave-request-v1",
                "LeaveRequest",
                leaveRequest.Id,
                request.EmployeeId,
                cancellationToken);

            if (workflowInstanceId.HasValue)
                leaveRequest.LinkWorkflow(workflowInstanceId.Value);
        }
        else
        {
            // Auto-approve for types that don't require approval
            leaveRequest.Submit();
            leaveRequest.Approve();
        }

        await leaveRequestRepository.AddAsync(leaveRequest, cancellationToken);

        return leaveRequest.Id;
    }
}

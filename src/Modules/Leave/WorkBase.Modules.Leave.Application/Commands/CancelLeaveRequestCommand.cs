using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record CancelLeaveRequestCommand(Guid RequestId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CancelLeaveRequestHandler(
    ILeaveRequestRepository requestRepository,
    ILeaveBalanceRepository balanceRepository) : ICommandHandler<CancelLeaveRequestCommand>
{
    public async Task<Result> Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await requestRepository.GetByIdAsync(request.RequestId, cancellationToken);
        if (leaveRequest is null || leaveRequest.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("LeaveRequest.NotFound", "Leave request not found"));

        var balance = await balanceRepository.GetAsync(
            leaveRequest.TenantId, leaveRequest.EmployeeId, leaveRequest.LeaveTypeId,
            leaveRequest.StartDate.Year, cancellationToken);

        leaveRequest.Cancel();

        if (balance is not null)
            balance.RemovePending(leaveRequest.TotalDays);

        requestRepository.Update(leaveRequest);
        return Result.Success();
    }
}

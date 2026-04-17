using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Leave.Domain.Events;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed class AdjustLeaveBalanceHandler(
    ILeaveBalanceRepository balanceRepository,
    ILeaveTypeRepository leaveTypeRepository)
    : ICommandHandler<AdjustLeaveBalanceCommand>
{
    public async Task<Result> Handle(AdjustLeaveBalanceCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await leaveTypeRepository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (leaveType is null)
            return Result.Failure(Error.NotFound("Leave.TypeNotFound",
                $"Typ nieobecności o id '{request.LeaveTypeId}' nie został znaleziony."));

        var balance = await balanceRepository.GetAsync(
            request.TenantId, request.EmployeeId, request.LeaveTypeId, request.Year, cancellationToken);

        if (balance is null)
        {
            // Create new balance
            balance = LeaveBalance.Create(
                request.TenantId,
                request.EmployeeId,
                request.LeaveTypeId,
                request.Year,
                request.NewTotalDays);

            balance.RaiseDomainEvent(new LeaveBalanceAdjustedEvent(
                balance.Id, request.TenantId, request.EmployeeId,
                request.LeaveTypeId, request.Year, 0, request.NewTotalDays));

            await balanceRepository.AddAsync(balance, cancellationToken);
        }
        else
        {
            var oldTotal = balance.TotalDays;
            balance.AdjustTotal(request.NewTotalDays);

            balance.RaiseDomainEvent(new LeaveBalanceAdjustedEvent(
                balance.Id, request.TenantId, request.EmployeeId,
                request.LeaveTypeId, request.Year, oldTotal, request.NewTotalDays));

            balanceRepository.Update(balance);
        }

        return Result.Success();
    }
}

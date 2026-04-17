using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record GetLeaveBalancesQuery(Guid EmployeeId, int Year)
    : IQuery<List<LeaveBalanceDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeaveBalancesHandler(
    ILeaveBalanceRepository balanceRepository,
    ILeaveTypeRepository leaveTypeRepository)
    : IQueryHandler<GetLeaveBalancesQuery, List<LeaveBalanceDto>>
{
    public async Task<Result<List<LeaveBalanceDto>>> Handle(
        GetLeaveBalancesQuery request, CancellationToken cancellationToken)
    {
        var balances = await balanceRepository.GetByEmployeeAsync(
            request.TenantId, request.EmployeeId, request.Year, cancellationToken);

        var types = await leaveTypeRepository.GetActiveByTenantAsync(request.TenantId, cancellationToken);
        var typeMap = types.ToDictionary(t => t.Id);

        var dtos = balances
            .Where(b => typeMap.ContainsKey(b.LeaveTypeId))
            .Select(b =>
            {
                var t = typeMap[b.LeaveTypeId];
                return new LeaveBalanceDto(
                    b.Id, b.LeaveTypeId, t.Code, t.Name, t.Color,
                    b.Year, b.TotalDays, b.UsedDays, b.PendingDays,
                    b.CarriedOverDays, b.RemainingDays);
            })
            .ToList();

        return dtos;
    }
}

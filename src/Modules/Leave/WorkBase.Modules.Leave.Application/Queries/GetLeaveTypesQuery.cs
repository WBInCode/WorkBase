using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record GetLeaveTypesQuery : IQuery<List<LeaveTypeDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeaveTypesHandler(ILeaveTypeRepository leaveTypeRepository)
    : IQueryHandler<GetLeaveTypesQuery, List<LeaveTypeDto>>
{
    public async Task<Result<List<LeaveTypeDto>>> Handle(GetLeaveTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await leaveTypeRepository.GetActiveByTenantAsync(request.TenantId, cancellationToken);

        var dtos = types
            .OrderBy(t => t.SortOrder)
            .Select(t => new LeaveTypeDto(
                t.Id, t.Code, t.Name, t.Description,
                t.IsPaid, t.RequiresApproval,
                t.DefaultDaysPerYear, t.Color, t.SortOrder))
            .ToList();

        return dtos;
    }
}

using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record GetLeaveRequestsQuery(Guid EmployeeId, int? Year = null)
    : IQuery<List<LeaveRequestDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeaveRequestsHandler(
    ILeaveRequestRepository requestRepository,
    ILeaveTypeRepository leaveTypeRepository)
    : IQueryHandler<GetLeaveRequestsQuery, List<LeaveRequestDto>>
{
    public async Task<Result<List<LeaveRequestDto>>> Handle(
        GetLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await requestRepository.GetByEmployeeAsync(
            request.TenantId, request.EmployeeId, cancellationToken);

        if (request.Year.HasValue)
            requests = requests.Where(r => r.StartDate.Year == request.Year.Value).ToList();

        var types = await leaveTypeRepository.GetActiveByTenantAsync(request.TenantId, cancellationToken);
        var typeMap = types.ToDictionary(t => t.Id);

        var dtos = requests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r =>
            {
                var hasType = typeMap.TryGetValue(r.LeaveTypeId, out var t);
                return new LeaveRequestDto(
                    r.Id, r.EmployeeId, r.LeaveTypeId,
                    hasType ? t!.Code : "?",
                    hasType ? t!.Name : "Nieznany",
                    hasType ? t!.Color : null,
                    r.StartDate, r.EndDate, r.TotalDays,
                    r.Status.ToString(),
                    r.Reason, r.CreatedAt);
            })
            .ToList();

        return dtos;
    }
}

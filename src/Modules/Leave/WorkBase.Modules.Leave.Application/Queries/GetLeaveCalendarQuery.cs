using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record GetLeaveCalendarQuery(
    IReadOnlyList<Guid> EmployeeIds,
    DateTime From,
    DateTime To) : IQuery<List<LeaveCalendarEntryDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetLeaveCalendarHandler(
    ILeaveCalendarEntryRepository calendarRepository,
    ILeaveTypeRepository leaveTypeRepository)
    : IQueryHandler<GetLeaveCalendarQuery, List<LeaveCalendarEntryDto>>
{
    public async Task<Result<List<LeaveCalendarEntryDto>>> Handle(
        GetLeaveCalendarQuery request, CancellationToken cancellationToken)
    {
        var entries = await calendarRepository.GetByTeamAsync(
            request.TenantId, request.EmployeeIds, request.From, request.To, cancellationToken);

        var types = await leaveTypeRepository.GetActiveByTenantAsync(request.TenantId, cancellationToken);
        var typeMap = types.ToDictionary(t => t.Id);

        var dtos = entries
            .OrderBy(e => e.Date)
            .ThenBy(e => e.EmployeeId)
            .Select(e =>
            {
                var hasType = typeMap.TryGetValue(e.LeaveTypeId, out var t);
                return new LeaveCalendarEntryDto(
                    e.EmployeeId, e.LeaveTypeId,
                    hasType ? t!.Code : "?",
                    hasType ? t!.Name : "Nieznany",
                    hasType ? t!.Color : null,
                    e.Date, e.DayFraction);
            })
            .ToList();

        return dtos;
    }
}

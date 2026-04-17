using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Queries;

public sealed record CheckLeaveConflictQuery(
    Guid EmployeeId, DateTime StartDate, DateTime EndDate, Guid? ExcludeRequestId = null)
    : IQuery<LeaveConflictResultDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record LeaveConflictResultDto(bool HasConflict, string? Message);

public sealed class CheckLeaveConflictHandler(ILeaveRequestRepository requestRepository)
    : IQueryHandler<CheckLeaveConflictQuery, LeaveConflictResultDto>
{
    public async Task<Result<LeaveConflictResultDto>> Handle(
        CheckLeaveConflictQuery request, CancellationToken cancellationToken)
    {
        var hasOverlap = await requestRepository.HasOverlappingRequestAsync(
            request.TenantId, request.EmployeeId,
            request.StartDate, request.EndDate,
            request.ExcludeRequestId, cancellationToken);

        return new LeaveConflictResultDto(
            hasOverlap,
            hasOverlap ? "Istnieje już wniosek urlopowy w podanym okresie." : null);
    }
}

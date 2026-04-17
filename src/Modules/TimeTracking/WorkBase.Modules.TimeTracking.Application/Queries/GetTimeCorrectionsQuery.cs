using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed record GetTimeCorrectionsQuery(
    Guid EmployeeId,
    DateOnly From,
    DateOnly To) : IQuery<List<TimeCorrectionDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record TimeCorrectionDto(
    Guid Id,
    Guid EmployeeId,
    DateOnly Date,
    DateTime OriginalClockIn,
    DateTime OriginalClockOut,
    DateTime CorrectedClockIn,
    DateTime CorrectedClockOut,
    string Reason,
    string CorrectedBy,
    string Status,
    DateTime CreatedAt);

public sealed class GetTimeCorrectionsHandler(ITimeCorrectionRepository repository)
    : IQueryHandler<GetTimeCorrectionsQuery, List<TimeCorrectionDto>>
{
    public async Task<Result<List<TimeCorrectionDto>>> Handle(GetTimeCorrectionsQuery request, CancellationToken cancellationToken)
    {
        var corrections = await repository.GetByEmployeeAsync(
            request.TenantId, request.EmployeeId, request.From, request.To, cancellationToken);

        var dtos = corrections.Select(c => new TimeCorrectionDto(
            c.Id, c.EmployeeId, c.Date,
            c.OriginalClockIn, c.OriginalClockOut,
            c.CorrectedClockIn, c.CorrectedClockOut,
            c.Reason, c.CorrectedBy,
            c.Status.ToString(), c.CreatedAt)).ToList();

        return dtos;
    }
}

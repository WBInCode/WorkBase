using WorkBase.Modules.Dashboard.Application.Contracts;
using WorkBase.Modules.Dashboard.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Dashboard.Application.Queries;

/// <summary>
/// <paramref name="VisibleEmployeeIds"/>: <c>null</c> = bez ograniczenia, pusty zbior = nikt.
///
/// Parametr NIE ma wartosci domyslnej celowo. Poprzednia wersja miala i wszystkie osiem
/// endpointow wolalo <c>new GetDashboardSummaryQuery()</c> bez zakresu, przez co kazdy
/// pracownik widzial liczby calej firmy. Bez domyslnej wartosci takie wywolanie sie nie
/// kompiluje — to tansze i pewniejsze niz test pilnujacy, zeby nikt o zakresie nie zapomnial.
/// </summary>
public sealed record GetDashboardSummaryQuery(IReadOnlyCollection<Guid>? VisibleEmployeeIds) : IQuery<DashboardSummaryDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetDashboardSummaryHandler(IDashboardQueryService queryService)
    : IQueryHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<Result<DashboardSummaryDto>> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await queryService.GetSummaryAsync(
            request.TenantId, request.VisibleEmployeeIds, cancellationToken);
        return summary;
    }
}

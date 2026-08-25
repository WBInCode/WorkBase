using WorkBase.Modules.Dashboard.Application.Dtos;

namespace WorkBase.Modules.Dashboard.Application.Contracts;

public interface IDashboardQueryService
{
    /// <summary>
    /// <paramref name="visibleEmployeeIds"/>: <c>null</c> = bez ograniczenia (zakres calej firmy),
    /// pusty zbior = uzytkownik nie widzi nikogo. Nie mylic tych dwoch — od tego zalezy, czy
    /// szeregowy pracownik zobaczy liczby calej firmy.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(Guid tenantId, IReadOnlyCollection<Guid>? visibleEmployeeIds, CancellationToken cancellationToken = default);
}

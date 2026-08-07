using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

/// <summary>
/// Przelicza karty czasu na nowo z odbić. Potrzebne, bo karty zapisane przed
/// poprawką kalkulatora trzymają sumy dłuższe niż doba (produkcja miała wpisy „30 dni”).
/// </summary>
public sealed record RecalculateTimeSheetsCommand(
    DateOnly From,
    DateOnly To,
    Guid? EmployeeId = null) : ICommand<RecalculateTimeSheetsResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed record RecalculateTimeSheetsResult(
    int SprawdzonychKart,
    int PoprawionychKart,
    double OdjetychGodzin,
    IReadOnlyList<RecalculatedDayDto> Poprawione);

public sealed record RecalculatedDayDto(
    Guid EmployeeId,
    DateOnly Date,
    double PrzedGodzin,
    double PoGodzin);

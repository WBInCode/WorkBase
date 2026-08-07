using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Contracts;

/// <summary>Odczyt kart czasu wielu pracowników naraz — na potrzeby przeliczania ewidencji.</summary>
public interface ITimeSheetBulkReader
{
    Task<List<TimeSheet>> GetSheetsAsync(
        Guid tenantId,
        Guid? employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}

using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Contracts;

public interface ILeaveCalendarEntryRepository
{
    Task<List<LeaveCalendarEntry>> GetByTeamAsync(Guid tenantId, IReadOnlyList<Guid> employeeIds,
        DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

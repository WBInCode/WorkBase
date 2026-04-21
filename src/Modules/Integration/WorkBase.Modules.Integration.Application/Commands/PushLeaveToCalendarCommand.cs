using WorkBase.Modules.Integration.Application.Adapters;
using WorkBase.Modules.Integration.Domain.Enums;
using WorkBase.Shared.Cqrs;

namespace WorkBase.Modules.Integration.Application.Commands;

public sealed record PushLeaveToCalendarCommand(
    IntegrationProvider Provider,
    string EmployeeName,
    string LeaveType,
    DateTime StartDate,
    DateTime EndDate) : ICommand<CalendarEventResult>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Modules.TimeTracking.Application.Services;
using WorkBase.Modules.TimeTracking.Domain.Events;

namespace WorkBase.Modules.TimeTracking.Application.EventHandlers;

public sealed class OrgUnitScheduleChangedEventHandler(
    OrgUnitScheduleGeneratorService generatorService,
    ILogger<OrgUnitScheduleChangedEventHandler> logger) : INotificationHandler<OrgUnitScheduleChangedEvent>
{
    public async Task Handle(OrgUnitScheduleChangedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "OrgUnitSchedule {Id} changed for OrgUnit {OrgUnitId} — regenerating schedules",
            notification.OrgUnitScheduleId, notification.OrgUnitId);

        await generatorService.GenerateForOrgUnitAsync(
            notification.TenantId,
            notification.OrgUnitScheduleId,
            cancellationToken: cancellationToken);
    }
}

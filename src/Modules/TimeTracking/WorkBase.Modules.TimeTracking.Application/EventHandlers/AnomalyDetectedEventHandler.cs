using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Events;

namespace WorkBase.Modules.TimeTracking.Application.EventHandlers;

/// <summary>
/// Handles AnomalyDetectedEvent by notifying the employee's supervisor (manager).
/// When the Notification module (SignalR/email) is available, this will dispatch
/// through INotificationService. For now, logs the notification intent.
/// </summary>
public sealed class AnomalyDetectedEventHandler(
    ISupervisorLookupService supervisorLookup,
    ILogger<AnomalyDetectedEventHandler> logger) : INotificationHandler<AnomalyDetectedEvent>
{
    public async Task Handle(AnomalyDetectedEvent notification, CancellationToken cancellationToken)
    {
        var supervisorId = await supervisorLookup.GetSupervisorEmployeeIdAsync(
            notification.EmployeeId, cancellationToken);

        if (supervisorId is null)
        {
            logger.LogDebug(
                "No supervisor found for employee {EmployeeId} — skipping anomaly notification for {AnomalyType} on {Date}",
                notification.EmployeeId, notification.AnomalyType, notification.Date);
            return;
        }

        // TODO: When Notification module is ready, dispatch via INotificationService:
        //   await notificationService.SendAsync(supervisorId.Value, "anomaly_detected", payload, cancellationToken);
        logger.LogInformation(
            "Anomaly notification: type={AnomalyType}, employee={EmployeeId}, date={Date}, supervisor={SupervisorId}, anomalyId={AnomalyId}",
            notification.AnomalyType, notification.EmployeeId, notification.Date, supervisorId.Value, notification.AnomalyId);
    }
}

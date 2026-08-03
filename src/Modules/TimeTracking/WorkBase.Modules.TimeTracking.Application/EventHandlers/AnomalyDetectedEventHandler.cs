using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Events;

namespace WorkBase.Modules.TimeTracking.Application.EventHandlers;

/// <summary>
/// Handles AnomalyDetectedEvent by notifying the employee's supervisor (manager)
/// via INotificationService (in-app + SignalR push).
/// </summary>
public sealed class AnomalyDetectedEventHandler(
    ISupervisorLookupService supervisorLookup,
    IOrganizationLookupService organizationLookup,
    INotificationService notificationService,
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

        // Dzwonek pyta o powiadomienia identyfikatorem konta, a nie pracownika — wczesniej
        // trafial tu identyfikator pracownika i powiadomienia o anomaliach nie docieraly do nikogo.
        var supervisorUserId = await organizationLookup.GetUserIdByEmployeeIdAsync(
            supervisorId.Value, cancellationToken);
        if (supervisorUserId is null)
        {
            logger.LogDebug(
                "Supervisor {SupervisorId} has no user account — skipping anomaly notification",
                supervisorId.Value);
            return;
        }

        await notificationService.SendAsync(
            notification.TenantId,
            supervisorUserId.Value,
            $"Anomalia: {notification.AnomalyType}",
            $"Wykryto anomalię typu {notification.AnomalyType} dla pracownika {notification.EmployeeId} w dniu {notification.Date:yyyy-MM-dd}.",
            "anomaly_detected",
            "anomaly",
            notification.AnomalyId,
            cancellationToken);

        logger.LogInformation(
            "Anomaly notification sent: type={AnomalyType}, employee={EmployeeId}, date={Date}, supervisor={SupervisorId}",
            notification.AnomalyType, notification.EmployeeId, notification.Date, supervisorId.Value);
    }
}

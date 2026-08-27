using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;
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

        var pracownik = await organizationLookup.GetEmployeeFullNameAsync(
            notification.EmployeeId, cancellationToken) ?? notification.EmployeeId.ToString();
        var rodzaj = OpisRodzaju(notification.AnomalyType);

        await notificationService.SendFromTemplateAsync(
            notification.TenantId,
            supervisorUserId.Value,
            templateCode: "anomaly_detected",
            variables: new Dictionary<string, string?>
            {
                ["rodzaj"] = rodzaj,
                ["pracownik"] = pracownik,
                ["data"] = notification.Date.ToString("dd.MM.yyyy"),
            },
            fallbackTitle: $"Anomalia: {rodzaj}",
            fallbackBody: $"{pracownik}: {rodzaj} w dniu {notification.Date:dd.MM.yyyy}.",
            category: "anomaly_detected",
            referenceType: "anomaly",
            referenceId: notification.AnomalyId,
            ct: cancellationToken);

        logger.LogInformation(
            "Anomaly notification sent: type={AnomalyType}, employee={EmployeeId}, date={Date}, supervisor={SupervisorId}",
            notification.AnomalyType, notification.EmployeeId, notification.Date, supervisorId.Value);
    }

    private static string OpisRodzaju(string typ) => typ switch
    {
        nameof(AnomalyType.MissingClockOut) => "brak wyjścia",
        nameof(AnomalyType.MissingClockIn) => "brak wejścia",
        nameof(AnomalyType.LateArrival) => "spóźnienie",
        nameof(AnomalyType.DoubleClockIn) => "podwójne wejście",
        nameof(AnomalyType.ExcessiveShift) => "za długa zmiana",
        nameof(AnomalyType.WorkOnDayOff) => "praca w dniu wolnym",
        _ => typ,
    };
}

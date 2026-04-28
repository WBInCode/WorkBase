using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Events;

namespace WorkBase.Modules.Organization.Application.EventHandlers;

public sealed class ProvisionKeycloakUserOnEmployeeCreated(
    IEmployeeRepository employeeRepository,
    IKeycloakAdminService keycloakAdmin,
    ILogger<ProvisionKeycloakUserOnEmployeeCreated> logger)
    : INotificationHandler<EmployeeCreatedEvent>
{
    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
        var employee = await employeeRepository.GetByIdAsync(notification.EmployeeId, cancellationToken);
        if (employee is null)
        {
            logger.LogWarning("Employee {Id} not found for Keycloak provisioning", notification.EmployeeId);
            return;
        }

        if (employee.UserId is not null)
        {
            logger.LogInformation("Employee {Id} already linked to Keycloak user {UserId}",
                employee.Id, employee.UserId);
            return;
        }

        var attributes = new Dictionary<string, string>
        {
            ["tenant_id"] = notification.TenantId.ToString(),
            ["employee_id"] = employee.Id.ToString()
        };

        var keycloakUserId = await keycloakAdmin.CreateUserAsync(
            employee.Email,
            employee.FirstName,
            employee.LastName,
            temporaryPassword: null,
            attributes,
            cancellationToken);

        if (keycloakUserId is null)
        {
            logger.LogWarning("Failed to create Keycloak user for employee {Id} ({Email})",
                employee.Id, employee.Email);
            return;
        }

        employee.LinkUser(Guid.Parse(keycloakUserId));
        employeeRepository.Update(employee);

        logger.LogInformation(
            "Auto-provisioned Keycloak user {KeycloakUserId} for employee {EmployeeId} ({Email})",
            keycloakUserId, employee.Id, employee.Email);
    }
}

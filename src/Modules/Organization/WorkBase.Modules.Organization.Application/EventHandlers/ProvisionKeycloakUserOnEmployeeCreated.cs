using MediatR;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.Organization.Application.Contracts;
using WorkBase.Modules.Organization.Domain.Events;

namespace WorkBase.Modules.Organization.Application.EventHandlers;

public sealed class ProvisionKeycloakUserOnEmployeeCreated(
    IEmployeeRepository employeeRepository,
    ITenantRepository tenantRepository,
    IKeycloakAdminService keycloakAdmin,
    ILogger<ProvisionKeycloakUserOnEmployeeCreated> logger)
    : INotificationHandler<EmployeeCreatedEvent>
{
    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
        try
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

            // Multi-realm: employees of a tenant with a dedicated Keycloak realm must be
            // created IN that realm (with the standard user role) — the shared-realm
            // CreateUserAsync would put them where they can never log in from the tenant's
            // login link. Tenants without a dedicated realm keep the shared-realm path.
            var tenant = await tenantRepository.GetByIdAsync(notification.TenantId, cancellationToken);
            var realmName = tenant?.KeycloakRealmName;

            var keycloakUserId = realmName is not null
                ? await keycloakAdmin.CreateUserInRealmAsync(
                    realmName,
                    employee.Email,
                    employee.FirstName,
                    employee.LastName,
                    temporaryPassword: null,
                    attributes,
                    realmRoles: ["workbase-user"],
                    cancellationToken)
                : await keycloakAdmin.CreateUserAsync(
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

            // This handler runs AFTER the command's transaction was committed (domain events
            // are dispatched from SavedChangesAsync), so UnitOfWorkBehavior will not save for
            // us — without this explicit save the UserId link is silently lost.
            await employeeRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Auto-provisioned Keycloak user {KeycloakUserId} for employee {EmployeeId} ({Email})",
                keycloakUserId, employee.Id, employee.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Keycloak provisioning failed for employee {Id}. Employee was still created.",
                notification.EmployeeId);
        }
    }
}

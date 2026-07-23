using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class HubEmployeeIdentityLinker(
    WorkBaseDbContext dbContext,
    ILogger<HubEmployeeIdentityLinker> logger) : IHubEmployeeIdentityLinker
{
    public async Task<HubEmployeeSsoDecision> ResolveForSsoAsync(
        Guid tenantId,
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var employee = await dbContext.Set<Employee>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.Email.ToLower() == normalizedEmail,
                cancellationToken);

        return employee is null
            ? new HubEmployeeSsoDecision(null, AccessDenied: false)
            : new HubEmployeeSsoDecision(
                employee.Id,
                AccessDenied: employee.Status != EmployeeStatus.Active);
    }

    public async Task<bool> LinkOnSsoAsync(
        Guid tenantId,
        Guid employeeId,
        string hubUserId,
        string keycloakUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(keycloakUserId, out var keycloakId))
        {
            logger.LogWarning("Keycloak user id {KeycloakUserId} is not a UUID", keycloakUserId);
            return false;
        }

        var employee = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.Id == employeeId,
                cancellationToken);
        if (employee is null || employee.Status != EmployeeStatus.Active)
            return false;

        if (employee.UserId is not null && employee.UserId != keycloakId)
        {
            logger.LogError(
                "Employee {EmployeeId} is already linked to another Keycloak user {KeycloakUserId}",
                employee.Id, employee.UserId);
            return false;
        }

        if (employee.UserId is null)
            employee.LinkUser(keycloakId);

        var accessRequest = await dbContext.Set<HubEmployeeAccessRequest>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.EmployeeId == employee.Id,
                cancellationToken);
        accessRequest?.MarkActive(hubUserId);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Linked HUB user {HubUserId} and Keycloak user {KeycloakUserId} to employee {EmployeeId}",
            hubUserId, keycloakUserId, employee.Id);
        return true;
    }
}
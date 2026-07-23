namespace WorkBase.Contracts;

public interface IHubEmployeeIdentityLinker
{
    Task<HubEmployeeSsoDecision> ResolveForSsoAsync(
        Guid tenantId,
        string email,
        string? employeeReference = null,
        string? hubUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> LinkOnSsoAsync(
        Guid tenantId,
        Guid employeeId,
        string hubUserId,
        string keycloakUserId,
        CancellationToken cancellationToken = default);
}

public sealed record HubEmployeeSsoDecision(Guid? EmployeeId, bool AccessDenied);
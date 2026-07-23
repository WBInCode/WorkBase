namespace WorkBase.Contracts;

/// <summary>
/// Adds an employee-access request to the current unit of work. Implementations must be
/// idempotent so employee creation and the access request can be committed atomically.
/// </summary>
public interface IEmployeeAccessProvisioningQueue
{
    Task QueueInvitationAsync(
        EmployeeAccessInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task QueueRevocationAsync(
        Guid tenantId,
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeAccessInvitationRequest(
    Guid TenantId,
    Guid EmployeeId,
    string Email,
    string FirstName,
    string LastName);
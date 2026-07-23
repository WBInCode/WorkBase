using WorkBase.Shared.Domain;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class HubEmployeeAccessRequest : Entity<Guid>, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string HubOrganizationId { get; private set; } = null!;
    public string HubProductInstanceId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public HubEmployeeAccessOperation Operation { get; private set; }
    public HubEmployeeAccessStatus Status { get; private set; }
    public string? HubInvitationId { get; private set; }
    public string? HubMembershipId { get; private set; }
    public string? HubUserId { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private HubEmployeeAccessRequest() { }

    public static HubEmployeeAccessRequest Create(
        Guid tenantId,
        Guid employeeId,
        string hubOrganizationId,
        string hubProductInstanceId,
        string email,
        string firstName,
        string lastName)
    {
        var now = DateTime.UtcNow;
        return new HubEmployeeAccessRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            HubOrganizationId = hubOrganizationId,
            HubProductInstanceId = hubProductInstanceId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            Operation = HubEmployeeAccessOperation.Invite,
            Status = HubEmployeeAccessStatus.Pending,
            NextAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void MarkProcessing()
    {
        Status = HubEmployeeAccessStatus.Processing;
        Attempts++;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkInvited(
        string? invitationId,
        string? membershipId,
        string? hubUserId,
        bool membershipActive)
    {
        HubInvitationId = invitationId;
        HubMembershipId = membershipId;
        HubUserId = hubUserId;
        Status = membershipActive ? HubEmployeeAccessStatus.Active : HubEmployeeAccessStatus.Invited;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkActive(string hubUserId, string? membershipId = null)
    {
        HubUserId = hubUserId;
        HubMembershipId = membershipId ?? HubMembershipId;
        Status = HubEmployeeAccessStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void QueueRevocation()
    {
        Operation = HubEmployeeAccessOperation.Revoke;
        Status = HubEmployeeAccessStatus.RevocationPending;
        NextAttemptAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkRevoked()
    {
        Status = HubEmployeeAccessStatus.Revoked;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Status = HubEmployeeAccessStatus.Failed;
        LastError = error.Length <= 2048 ? error : error[..2048];
        var delayMinutes = Math.Min(Math.Pow(2, Math.Min(Attempts, 10)), 24 * 60);
        NextAttemptAt = DateTime.UtcNow.AddMinutes(delayMinutes);
        UpdatedAt = DateTime.UtcNow;
    }

    public void RetryNow()
    {
        Attempts = 0;
        Status = Operation == HubEmployeeAccessOperation.Revoke
            ? HubEmployeeAccessStatus.RevocationPending
            : HubEmployeeAccessStatus.Pending;
        NextAttemptAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        LastError = null;
    }
}

public enum HubEmployeeAccessStatus
{
    Pending = 0,
    Processing = 1,
    Invited = 2,
    Active = 3,
    Failed = 4,
    RevocationPending = 5,
    Revoked = 6,
}

public enum HubEmployeeAccessOperation
{
    Invite = 0,
    Revoke = 1,
}
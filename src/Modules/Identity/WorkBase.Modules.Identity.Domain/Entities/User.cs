namespace WorkBase.Modules.Identity.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string KeycloakId { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }

    private User() { }

    public static User Create(
        string keycloakId,
        string email,
        string firstName,
        string lastName,
        Guid tenantId)
    {
        return new User
        {
            KeycloakId = keycloakId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            TenantId = tenantId,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string email, string firstName, string lastName)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
}

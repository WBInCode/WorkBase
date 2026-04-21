using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Domain.Entities;

public sealed class ContactPerson : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ContactId { get; private set; }
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string? Position { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsPrimary { get; private set; }

    private ContactPerson() { }

    public static ContactPerson Create(
        Guid tenantId, Guid contactId, string firstName, string lastName,
        string? position = null, string? email = null, string? phone = null,
        bool isPrimary = false)
        => new()
        {
            TenantId = tenantId,
            ContactId = contactId,
            FirstName = firstName,
            LastName = lastName,
            Position = position,
            Email = email,
            Phone = phone,
            IsPrimary = isPrimary,
        };

    public void Update(string firstName, string lastName, string? position, string? email, string? phone, bool isPrimary)
    {
        FirstName = firstName;
        LastName = lastName;
        Position = position;
        Email = email;
        Phone = phone;
        IsPrimary = isPrimary;
    }
}

using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Domain.Entities;

public sealed class Contact : AuditableEntity<Guid>, ITenantScoped, IAuditable
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public ContactType Type { get; private set; }
    public string? Nip { get; private set; }
    public string? Regon { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Website { get; private set; }
    public string? Street { get; private set; }
    public string? City { get; private set; }
    public string? PostalCode { get; private set; }
    public string? Country { get; private set; }
    public string? Notes { get; private set; }
    public Guid? OwnerId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Contact() { }

    public static Contact Create(
        Guid tenantId, string name, ContactType type,
        string? nip = null, string? regon = null,
        string? email = null, string? phone = null,
        string? website = null, string? street = null,
        string? city = null, string? postalCode = null,
        string? country = null, string? notes = null,
        Guid? ownerId = null)
    {
        return new Contact
        {
            TenantId = tenantId,
            Name = name,
            Type = type,
            Nip = nip,
            Regon = regon,
            Email = email,
            Phone = phone,
            Website = website,
            Street = street,
            City = city,
            PostalCode = postalCode,
            Country = country,
            Notes = notes,
            OwnerId = ownerId,
        };
    }

    public void Update(
        string name, ContactType type, string? nip, string? regon,
        string? email, string? phone, string? website,
        string? street, string? city, string? postalCode, string? country,
        string? notes)
    {
        Name = name;
        Type = type;
        Nip = nip;
        Regon = regon;
        Email = email;
        Phone = phone;
        Website = website;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        Notes = notes;
    }

    public void AssignOwner(Guid? ownerId) => OwnerId = ownerId;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

public enum ContactType
{
    Company = 0,
    Individual = 1,
    Government = 2,
    Other = 3,
}

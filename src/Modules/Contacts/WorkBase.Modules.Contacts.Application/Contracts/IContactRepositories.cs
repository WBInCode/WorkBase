using WorkBase.Modules.Contacts.Domain.Entities;

namespace WorkBase.Modules.Contacts.Application.Contracts;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Contact>> GetByTenantAsync(Guid tenantId, CancellationToken ct);
    Task<List<Contact>> GetByOwnerAsync(Guid tenantId, Guid ownerId, CancellationToken ct);
    Task<List<Contact>> SearchAsync(Guid tenantId, string query, CancellationToken ct);
    Task AddAsync(Contact contact, CancellationToken ct);
    void Update(Contact contact);
    void Remove(Contact contact);
}

public interface IContactPersonRepository
{
    Task<List<ContactPerson>> GetByContactAsync(Guid contactId, CancellationToken ct);
    Task<ContactPerson?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(ContactPerson person, CancellationToken ct);
    void Update(ContactPerson person);
    void Remove(ContactPerson person);
}

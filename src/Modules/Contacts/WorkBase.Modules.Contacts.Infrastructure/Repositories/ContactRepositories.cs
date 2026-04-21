using Microsoft.EntityFrameworkCore;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Contacts.Application.Contracts;
using WorkBase.Modules.Contacts.Domain.Entities;

namespace WorkBase.Modules.Contacts.Infrastructure.Repositories;

public sealed class ContactRepository(WorkBaseDbContext db) : IContactRepository
{
    public async Task<Contact?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Set<Contact>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<Contact>> GetByTenantAsync(Guid tenantId, CancellationToken ct)
        => await db.Set<Contact>().Where(e => e.TenantId == tenantId).OrderBy(e => e.Name).ToListAsync(ct);

    public async Task<List<Contact>> GetByOwnerAsync(Guid tenantId, Guid ownerId, CancellationToken ct)
        => await db.Set<Contact>().Where(e => e.TenantId == tenantId && e.OwnerId == ownerId).OrderBy(e => e.Name).ToListAsync(ct);

    public async Task<List<Contact>> SearchAsync(Guid tenantId, string query, CancellationToken ct)
        => await db.Set<Contact>()
            .Where(e => e.TenantId == tenantId && (e.Name.Contains(query) || (e.Nip != null && e.Nip.Contains(query)) || (e.Email != null && e.Email.Contains(query))))
            .OrderBy(e => e.Name).ToListAsync(ct);

    public async Task AddAsync(Contact contact, CancellationToken ct) => await db.Set<Contact>().AddAsync(contact, ct);
    public void Update(Contact contact) => db.Set<Contact>().Update(contact);
    public void Remove(Contact contact) => db.Set<Contact>().Remove(contact);
}

public sealed class ContactPersonRepository(WorkBaseDbContext db) : IContactPersonRepository
{
    public async Task<List<ContactPerson>> GetByContactAsync(Guid contactId, CancellationToken ct)
        => await db.Set<ContactPerson>().Where(e => e.ContactId == contactId).OrderBy(e => e.LastName).ToListAsync(ct);

    public async Task<ContactPerson?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Set<ContactPerson>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(ContactPerson person, CancellationToken ct) => await db.Set<ContactPerson>().AddAsync(person, ct);
    public void Update(ContactPerson person) => db.Set<ContactPerson>().Update(person);
    public void Remove(ContactPerson person) => db.Set<ContactPerson>().Remove(person);
}

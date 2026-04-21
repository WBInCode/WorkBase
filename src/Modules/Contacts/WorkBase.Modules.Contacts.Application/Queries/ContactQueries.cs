using WorkBase.Modules.Contacts.Application.Contracts;
using WorkBase.Modules.Contacts.Application.Dtos;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Application.Queries;

public sealed record GetContactsQuery(Guid? OwnerId = null, string? Search = null) : IQuery<List<ContactDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetContactsHandler(IContactRepository repo) : IQueryHandler<GetContactsQuery, List<ContactDto>>
{
    public async Task<Result<List<ContactDto>>> Handle(GetContactsQuery request, CancellationToken ct)
    {
        var contacts = !string.IsNullOrWhiteSpace(request.Search)
            ? await repo.SearchAsync(request.TenantId, request.Search, ct)
            : request.OwnerId.HasValue
                ? await repo.GetByOwnerAsync(request.TenantId, request.OwnerId.Value, ct)
                : await repo.GetByTenantAsync(request.TenantId, ct);

        return contacts.Select(c => new ContactDto(
            c.Id, c.Name, c.Type, c.Nip, c.Regon,
            c.Email, c.Phone, c.Website,
            c.Street, c.City, c.PostalCode, c.Country,
            c.Notes, c.OwnerId, c.IsActive,
            c.CreatedAt, c.ModifiedAt)).ToList();
    }
}

public sealed record GetContactByIdQuery(Guid ContactId) : IQuery<ContactDto>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetContactByIdHandler(IContactRepository repo) : IQueryHandler<GetContactByIdQuery, ContactDto>
{
    public async Task<Result<ContactDto>> Handle(GetContactByIdQuery request, CancellationToken ct)
    {
        var c = await repo.GetByIdAsync(request.ContactId, ct);
        if (c is null) return Result.Failure<ContactDto>(Error.NotFound("Contact.NotFound", "Kontrahent nie został znaleziony."));
        return new ContactDto(c.Id, c.Name, c.Type, c.Nip, c.Regon, c.Email, c.Phone, c.Website,
            c.Street, c.City, c.PostalCode, c.Country, c.Notes, c.OwnerId, c.IsActive, c.CreatedAt, c.ModifiedAt);
    }
}

public sealed record GetContactPersonsQuery(Guid ContactId) : IQuery<List<ContactPersonDto>>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class GetContactPersonsHandler(IContactPersonRepository repo) : IQueryHandler<GetContactPersonsQuery, List<ContactPersonDto>>
{
    public async Task<Result<List<ContactPersonDto>>> Handle(GetContactPersonsQuery request, CancellationToken ct)
    {
        var persons = await repo.GetByContactAsync(request.ContactId, ct);
        return persons.Select(p => new ContactPersonDto(p.Id, p.ContactId, p.FirstName, p.LastName, p.Position, p.Email, p.Phone, p.IsPrimary)).ToList();
    }
}

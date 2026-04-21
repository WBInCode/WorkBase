using WorkBase.Modules.Contacts.Application.Contracts;
using WorkBase.Modules.Contacts.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Contacts.Application.Commands;

public sealed record CreateContactCommand(
    string Name, ContactType Type, string? Nip, string? Regon,
    string? Email, string? Phone, string? Website,
    string? Street, string? City, string? PostalCode, string? Country,
    string? Notes, Guid? OwnerId) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateContactHandler(IContactRepository repo) : ICommandHandler<CreateContactCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateContactCommand request, CancellationToken ct)
    {
        var contact = Contact.Create(
            request.TenantId, request.Name, request.Type,
            request.Nip, request.Regon, request.Email, request.Phone,
            request.Website, request.Street, request.City, request.PostalCode,
            request.Country, request.Notes, request.OwnerId);
        await repo.AddAsync(contact, ct);
        return contact.Id;
    }
}

public sealed record UpdateContactCommand(
    Guid ContactId, string Name, ContactType Type, string? Nip, string? Regon,
    string? Email, string? Phone, string? Website,
    string? Street, string? City, string? PostalCode, string? Country,
    string? Notes) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateContactHandler(IContactRepository repo) : ICommandHandler<UpdateContactCommand>
{
    public async Task<Result> Handle(UpdateContactCommand request, CancellationToken ct)
    {
        var contact = await repo.GetByIdAsync(request.ContactId, ct);
        if (contact is null) return Result.Failure(Error.NotFound("Contact.NotFound", "Kontrahent nie został znaleziony."));
        contact.Update(request.Name, request.Type, request.Nip, request.Regon,
            request.Email, request.Phone, request.Website,
            request.Street, request.City, request.PostalCode, request.Country, request.Notes);
        repo.Update(contact);
        return Result.Success();
    }
}

public sealed record AssignContactOwnerCommand(Guid ContactId, Guid? OwnerId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AssignContactOwnerHandler(IContactRepository repo) : ICommandHandler<AssignContactOwnerCommand>
{
    public async Task<Result> Handle(AssignContactOwnerCommand request, CancellationToken ct)
    {
        var contact = await repo.GetByIdAsync(request.ContactId, ct);
        if (contact is null) return Result.Failure(Error.NotFound("Contact.NotFound", "Kontrahent nie został znaleziony."));
        contact.AssignOwner(request.OwnerId);
        repo.Update(contact);
        return Result.Success();
    }
}

public sealed record AddContactPersonCommand(
    Guid ContactId, string FirstName, string LastName,
    string? Position, string? Email, string? Phone, bool IsPrimary) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class AddContactPersonHandler(IContactRepository contactRepo, IContactPersonRepository personRepo) : ICommandHandler<AddContactPersonCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddContactPersonCommand request, CancellationToken ct)
    {
        var contact = await contactRepo.GetByIdAsync(request.ContactId, ct);
        if (contact is null) return Result.Failure<Guid>(Error.NotFound("Contact.NotFound", "Kontrahent nie został znaleziony."));
        var person = ContactPerson.Create(request.TenantId, request.ContactId,
            request.FirstName, request.LastName, request.Position,
            request.Email, request.Phone, request.IsPrimary);
        await personRepo.AddAsync(person, ct);
        return person.Id;
    }
}

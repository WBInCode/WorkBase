using WorkBase.Modules.Contacts.Domain.Entities;

namespace WorkBase.Modules.Contacts.Application.Dtos;

public sealed record ContactDto(
    Guid Id, string Name, ContactType Type, string? Nip, string? Regon,
    string? Email, string? Phone, string? Website,
    string? Street, string? City, string? PostalCode, string? Country,
    string? Notes, Guid? OwnerId, bool IsActive,
    DateTime CreatedAt, DateTime? ModifiedAt);

public sealed record ContactPersonDto(
    Guid Id, Guid ContactId, string FirstName, string LastName,
    string? Position, string? Email, string? Phone, bool IsPrimary);

namespace WorkBase.Modules.Integration.Application.Adapters;

public interface IContactSyncAdapter
{
    Task<List<ExternalContact>> GetContactsAsync(string accessToken, CancellationToken ct);
    Task<ExternalContact> CreateContactAsync(ExternalContact contact, string accessToken, CancellationToken ct);
    Task UpdateContactAsync(ExternalContact contact, string accessToken, CancellationToken ct);
    Task DeleteContactAsync(string externalId, string accessToken, CancellationToken ct);
}

public interface IContactSyncAdapterFactory
{
    IContactSyncAdapter Create(string provider);
}

public record ExternalContact(
    string? ExternalId, string FirstName, string LastName,
    string? Email, string? Phone, string? Company, string? JobTitle);

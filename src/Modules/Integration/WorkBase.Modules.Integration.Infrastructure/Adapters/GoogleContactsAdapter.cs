using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using WorkBase.Modules.Integration.Application.Adapters;

namespace WorkBase.Modules.Integration.Infrastructure.Adapters;

public sealed class GoogleContactsAdapter : IContactSyncAdapter
{
    private static readonly HttpClient Http = new();
    private const string ApiBase = "https://people.googleapis.com/v1";

    public async Task<List<ExternalContact>> GetContactsAsync(string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{ApiBase}/people/me/connections?personFields=names,emailAddresses,phoneNumbers,organizations&pageSize=100");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!json.TryGetProperty("connections", out var connections)) return [];
        return connections.EnumerateArray().Select(MapContact).ToList();
    }

    public async Task<ExternalContact> CreateContactAsync(ExternalContact contact, string accessToken, CancellationToken ct)
    {
        var body = BuildContactBody(contact);
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/people:createContact");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(body);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return MapContact(json);
    }

    public async Task UpdateContactAsync(ExternalContact contact, string accessToken, CancellationToken ct)
    {
        if (contact.ExternalId is null) return;
        var body = BuildContactBody(contact);
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"{ApiBase}/{contact.ExternalId}:updateContact?updatePersonFields=names,emailAddresses,phoneNumbers,organizations");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Content = JsonContent.Create(body);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteContactAsync(string externalId, string accessToken, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"{ApiBase}/{externalId}:deleteContact");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var resp = await Http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    private static ExternalContact MapContact(JsonElement p)
    {
        var names = p.TryGetProperty("names", out var n) ? n.EnumerateArray().FirstOrDefault() : default;
        var emails = p.TryGetProperty("emailAddresses", out var e) ? e.EnumerateArray().FirstOrDefault() : default;
        var phones = p.TryGetProperty("phoneNumbers", out var ph) ? ph.EnumerateArray().FirstOrDefault() : default;
        var orgs = p.TryGetProperty("organizations", out var o) ? o.EnumerateArray().FirstOrDefault() : default;
        return new ExternalContact(
            p.TryGetProperty("resourceName", out var rn) ? rn.GetString() : null,
            names.ValueKind != JsonValueKind.Undefined && names.TryGetProperty("givenName", out var gn) ? gn.GetString() ?? "" : "",
            names.ValueKind != JsonValueKind.Undefined && names.TryGetProperty("familyName", out var fn) ? fn.GetString() ?? "" : "",
            emails.ValueKind != JsonValueKind.Undefined && emails.TryGetProperty("value", out var ev) ? ev.GetString() : null,
            phones.ValueKind != JsonValueKind.Undefined && phones.TryGetProperty("value", out var pv) ? pv.GetString() : null,
            orgs.ValueKind != JsonValueKind.Undefined && orgs.TryGetProperty("name", out var on) ? on.GetString() : null,
            orgs.ValueKind != JsonValueKind.Undefined && orgs.TryGetProperty("title", out var ot) ? ot.GetString() : null);
    }

    private static object BuildContactBody(ExternalContact c) => new
    {
        names = new[] { new { givenName = c.FirstName, familyName = c.LastName } },
        emailAddresses = c.Email is not null ? new[] { new { value = c.Email } } : Array.Empty<object>(),
        phoneNumbers = c.Phone is not null ? new[] { new { value = c.Phone } } : Array.Empty<object>(),
        organizations = c.Company is not null ? new[] { new { name = c.Company, title = c.JobTitle } } : Array.Empty<object>(),
    };
}

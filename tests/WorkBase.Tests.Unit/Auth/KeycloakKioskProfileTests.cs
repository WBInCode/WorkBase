using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Auth;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public sealed class KeycloakKioskProfileTests
{
    [Fact]
    public async Task Existing_managed_kiosk_with_empty_profile_is_repaired_without_losing_required_actions()
    {
        var handler = new KioskProfileHandler();
        var service = CreateService(handler);

        var result = await service.PrepareKioskUserAsync(
            "workbase",
            "kiosk-acme",
            "Acme Company",
            "unused-temporary-password",
            new Dictionary<string, string>
            {
                ["tenant_id"] = "10000000-0000-0000-0000-000000000001",
                ["kiosk_location"] = "Glowna",
                ["kiosk_managed"] = "true",
            });

        Assert.NotNull(result);
        Assert.False(result.CredentialsIssued);
        Assert.NotNull(handler.ProfileUpdate);
        Assert.Equal("kiosk-acme", handler.ProfileUpdate.RootElement.GetProperty("username").GetString());
        Assert.Equal("kiosk-acme@workbase.local", handler.ProfileUpdate.RootElement.GetProperty("email").GetString());
        Assert.Equal("Kiosk", handler.ProfileUpdate.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Acme Company", handler.ProfileUpdate.RootElement.GetProperty("lastName").GetString());
        Assert.True(handler.ProfileUpdate.RootElement.GetProperty("emailVerified").GetBoolean());
        Assert.Contains(
            handler.ProfileUpdate.RootElement.GetProperty("requiredActions").EnumerateArray(),
            action => action.GetString() == "UPDATE_PASSWORD");
    }

    private static KeycloakAdminService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:AdminUrl"] = "https://keycloak.test",
                ["Keycloak:Admin:Username"] = "admin",
                ["Keycloak:Admin:Password"] = "secret",
            })
            .Build();
        return new KeycloakAdminService(
            new StubHttpClientFactory(handler),
            configuration,
            NullLogger<KeycloakAdminService>.Instance);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class KioskProfileHandler : HttpMessageHandler
    {
        private const string UserRepresentation = """
            {
              "id": "kiosk-user-1",
              "username": "kiosk-acme",
              "enabled": true,
              "emailVerified": true,
              "requiredActions": ["UPDATE_PASSWORD"],
              "attributes": {
                "tenant_id": ["10000000-0000-0000-0000-000000000001"],
                "kiosk_location": ["Glowna"],
                "kiosk_managed": ["true"],
                "kiosk_credentials_delivered": ["true"]
              }
            }
            """;

        public JsonDocument? ProfileUpdate { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (path.EndsWith("/realms/master/protocol/openid-connect/token", StringComparison.Ordinal))
                return Json(HttpStatusCode.OK, """{"access_token":"admin-token"}""");

            if (request.Method == HttpMethod.Get && path.Contains("?username=kiosk-acme", StringComparison.Ordinal))
                return Json(HttpStatusCode.OK, $"[{UserRepresentation}]");

            if (request.Method == HttpMethod.Get
                && path.EndsWith("/admin/realms/workbase/users/kiosk-user-1", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, UserRepresentation);
            }

            if (request.Method == HttpMethod.Put
                && path.EndsWith("/admin/realms/workbase/users/kiosk-user-1", StringComparison.Ordinal))
            {
                ProfileUpdate = JsonDocument.Parse(
                    await request.Content!.ReadAsStringAsync(cancellationToken));
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            if (request.Method == HttpMethod.Get
                && path.EndsWith("/admin/realms/workbase/roles/workbase-kiosk", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"id":"role-kiosk","name":"workbase-kiosk"}""");
            }

            if (request.Method == HttpMethod.Post
                && path.EndsWith("/admin/realms/workbase/users/kiosk-user-1/role-mappings/realm", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            throw new InvalidOperationException($"Unexpected Keycloak request: {request.Method} {path}");
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) =>
            new(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
    }
}

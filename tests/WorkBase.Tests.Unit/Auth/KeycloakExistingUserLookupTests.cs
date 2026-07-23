using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Infrastructure.Auth;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public sealed class KeycloakExistingUserLookupTests
{
    [Fact]
    public async Task Existing_username_is_reused_when_Keycloak_email_is_not_yet_synchronized()
    {
        const string loginEmail = "owner@example.test";
        var handler = new KeycloakHandler(loginEmail);
        var service = CreateService(handler);

        var userId = await service.CreateUserInRealmAsync(
            "workbase",
            loginEmail,
            "Owner",
            "Test",
            temporaryPassword: null,
            attributes: new Dictionary<string, string>
            {
                ["tenant_id"] = "00000000-0000-0000-0000-000000000001",
                ["hub_org_id"] = "org-1",
                ["hub_user_id"] = "hub-user-1",
            });

        Assert.Equal("keycloak-user-1", userId);
        Assert.True(handler.EmailLookupRequested);
        Assert.True(handler.UsernameLookupRequested);
        Assert.True(handler.IdentityScopeRequested);
    }

    [Fact]
    public async Task Ambiguous_email_lookup_is_rejected_without_username_fallback()
    {
        const string loginEmail = "owner@example.test";
        var handler = new KeycloakHandler(loginEmail, ambiguousEmail: true);
        var service = CreateService(handler);

        var userId = await service.CreateUserInRealmAsync(
            "workbase", loginEmail, "Owner", "Test", temporaryPassword: null);

        Assert.Null(userId);
        Assert.True(handler.EmailLookupRequested);
        Assert.False(handler.UsernameLookupRequested);
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

    private sealed class KeycloakHandler(string loginEmail, bool ambiguousEmail = false) : HttpMessageHandler
    {
        public bool EmailLookupRequested { get; private set; }
        public bool UsernameLookupRequested { get; private set; }
        public bool IdentityScopeRequested { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "";
            if (path.EndsWith("/realms/master/protocol/openid-connect/token", StringComparison.Ordinal))
                return Json(HttpStatusCode.OK, """{"access_token":"admin-token"}""");

            if (request.Method == HttpMethod.Post && path.EndsWith("/admin/realms/workbase/users", StringComparison.Ordinal))
                return Json(HttpStatusCode.Conflict, "{}");

            if (request.Method == HttpMethod.Get && path.Contains("?email=", StringComparison.Ordinal))
            {
                EmailLookupRequested = true;
                return Json(HttpStatusCode.OK, ambiguousEmail
                    ? $$"""[{"id":"one","email":"{{loginEmail}}"},{"id":"two","email":"{{loginEmail}}"}]"""
                    : "[]");
            }

            if (request.Method == HttpMethod.Get && path.Contains("?username=", StringComparison.Ordinal))
            {
                UsernameLookupRequested = true;
                return Json(HttpStatusCode.OK,
                    $$"""[{"id":"keycloak-user-1","username":"{{loginEmail}}","email":"legacy@example.test"}]""");
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("/admin/realms/workbase/users/keycloak-user-1", StringComparison.Ordinal))
            {
                IdentityScopeRequested = true;
                return Json(HttpStatusCode.OK,
                    """{"attributes":{"tenant_id":["00000000-0000-0000-0000-000000000001"],"hub_org_id":["org-1"],"hub_user_id":["hub-user-1"]}}""");
            }

            throw new InvalidOperationException($"Unexpected Keycloak request: {request.Method} {path}");
        }

        private static Task<HttpResponseMessage> Json(HttpStatusCode statusCode, string body) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
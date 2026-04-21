using System.Net;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// T-SEC-011: Integration tests verifying that permission enforcement
/// correctly blocks/allows API requests based on user permissions.
/// </summary>
[Collection("Integration")]
public sealed class PermissionEnforcementTests
{
    private readonly WorkBaseWebFactory _factory;

    private static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid UserA = Guid.Parse("20000000-0000-0000-0000-000000000001");

    public PermissionEnforcementTests(WorkBaseWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_request_to_protected_endpoint_returns_Unauthorized()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/api/iam/roles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_without_tenant_returns_Forbidden()
    {
        // User is authenticated but has no tenant_id claim
        var client = _factory.CreateAuthenticatedClient(userId: UserA, tenantId: null);

        var response = await client.GetAsync("/api/iam/roles");

        // PermissionEndpointFilter returns Forbid when tenantId is null
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_with_tenant_but_no_permission_returns_Forbidden()
    {
        var client = _factory.CreateAuthenticatedClient(
            userId: UserA,
            tenantId: TenantA,
            permissions: []);

        var response = await client.GetAsync("/api/iam/roles");

        // PermissionEndpointFilter blocks because user lacks 'identity.view'
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_permission_returns_Forbidden()
    {
        var client = _factory.CreateAuthenticatedClient(
            userId: UserA,
            tenantId: TenantA,
            permissions: ["leave.view"]); // Has leave.view but needs identity.view

        var response = await client.GetAsync("/api/iam/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Correct_permission_passes_filter()
    {
        var client = _factory.CreateAuthenticatedClient(
            userId: UserA,
            tenantId: TenantA,
            permissions: ["identity.view"]);

        var response = await client.GetAsync("/api/iam/roles");

        // Permission filter passed — response is NOT 401/403
        // The endpoint may return 200 (empty list) or 500 (DB/query issues with InMemory)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_does_not_require_auth()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/health");

        // Health endpoint is not protected by auth
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Root_endpoint_does_not_require_auth()
    {
        var client = _factory.CreateUnauthenticatedClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

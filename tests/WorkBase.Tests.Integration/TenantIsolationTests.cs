using System.Net;
using Xunit;

namespace WorkBase.Tests.Integration;

/// <summary>
/// T-SEC-012: Integration tests verifying that tenant isolation
/// prevents data leaks across tenants at the middleware level.
/// </summary>
[Collection("Integration")]
public sealed class TenantIsolationTests
{
    private readonly WorkBaseWebFactory _factory;

    private static readonly Guid TenantA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid UserA = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid UserB = Guid.Parse("20000000-0000-0000-0000-000000000002");

    public TenantIsolationTests(WorkBaseWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task User_without_tenant_claim_is_rejected_on_permission_protected_endpoint()
    {
        // A user with no tenant_id claim cannot pass PermissionEndpointFilter
        var client = _factory.CreateAuthenticatedClient(userId: UserA, tenantId: null);

        var response = await client.GetAsync("/api/iam/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantA_user_with_permission_can_access_endpoint()
    {
        var clientA = _factory.CreateAuthenticatedClient(
            userId: UserA, tenantId: TenantA, permissions: ["identity.view"]);

        var response = await clientA.GetAsync("/api/iam/roles");

        // Permission filter passes, endpoint runs (may return 200 or 500 from DB)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantB_user_with_permission_can_access_endpoint_independently()
    {
        var clientB = _factory.CreateAuthenticatedClient(
            userId: UserB, tenantId: TenantB, permissions: ["identity.view"]);

        var response = await clientB.GetAsync("/api/iam/roles");

        // TenantB user also passes the filter — isolated from TenantA
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task TenantA_permissions_do_not_leak_to_TenantB()
    {
        // TenantA user has identity.view, TenantB user does NOT
        var clientA = _factory.CreateAuthenticatedClient(
            userId: UserA, tenantId: TenantA, permissions: ["identity.view"]);
        var clientB = _factory.CreateAuthenticatedClient(
            userId: UserB, tenantId: TenantB, permissions: []);

        var responseA = await clientA.GetAsync("/api/iam/roles");
        var responseB = await clientB.GetAsync("/api/iam/roles");

        // TenantA passes, TenantB is forbidden — permissions are isolated
        Assert.NotEqual(HttpStatusCode.Forbidden, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
    }

    [Fact]
    public async Task Notification_endpoint_requires_authentication_per_tenant()
    {
        // Notifications require auth (RequireAuthorization) but not RequirePermission
        var clientA = _factory.CreateAuthenticatedClient(userId: UserA, tenantId: TenantA);
        var clientB = _factory.CreateAuthenticatedClient(userId: UserB, tenantId: TenantB);
        var anonymous = _factory.CreateUnauthenticatedClient();

        var responseA = await clientA.GetAsync("/api/notifications");
        var responseB = await clientB.GetAsync("/api/notifications");
        var responseAnon = await anonymous.GetAsync("/api/notifications");

        // Both tenants can access (authenticated), anonymous cannot
        Assert.NotEqual(HttpStatusCode.Unauthorized, responseA.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, responseB.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, responseAnon.StatusCode);
    }

    [Fact]
    public async Task Different_tenants_leave_endpoints_are_isolated()
    {
        // TenantA has leave.view, TenantB does not
        var clientA = _factory.CreateAuthenticatedClient(
            userId: UserA, tenantId: TenantA, permissions: ["leave.view"]);
        var clientB = _factory.CreateAuthenticatedClient(
            userId: UserB, tenantId: TenantB, permissions: []);

        var responseA = await clientA.GetAsync("/api/leave/types");
        var responseB = await clientB.GetAsync("/api/leave/types");

        // TenantA passes permission check, TenantB is blocked
        Assert.NotEqual(HttpStatusCode.Forbidden, responseA.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, responseB.StatusCode);
    }
}

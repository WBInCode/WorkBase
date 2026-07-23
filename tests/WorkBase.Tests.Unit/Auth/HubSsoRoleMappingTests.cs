using WorkBase.Infrastructure.HubPlatform;
using Xunit;

namespace WorkBase.Tests.Unit.Auth;

public sealed class HubSsoRoleMappingTests
{
    [Theory]
    [InlineData("OWNER", "MEMBER", "owner", "workbase-admin")]
    [InlineData("MEMBER", "OWNER", "owner", "workbase-admin")]
    [InlineData("ADMIN", "MEMBER", "admin", "workbase-user")]
    [InlineData("MEMBER", "ADMIN", "admin", "workbase-user")]
    [InlineData("MEMBER", "MEMBER", "member", "workbase-user")]
    [InlineData("", "", "member", "workbase-user")]
    public void MapsHubRolesByHighestPrivilege(
        string organizationRole,
        string instanceRole,
        string expectedHubRole,
        string expectedRealmRole)
    {
        Assert.Equal(expectedHubRole, HubSsoService.MapHubRole(organizationRole, instanceRole));
        Assert.Equal(expectedRealmRole, HubSsoService.MapRealmRole(organizationRole, instanceRole));
    }

    [Theory]
    [InlineData("owner", "00000000-0000-0000-0000-000000000001", "Super Admin")]
    [InlineData("owner", "10000000-0000-0000-0000-000000000001", "Admin")]
    [InlineData("admin", "00000000-0000-0000-0000-000000000001", "Pracownik")]
    [InlineData("admin", "10000000-0000-0000-0000-000000000001", "Pracownik")]
    [InlineData("member", "10000000-0000-0000-0000-000000000001", "Pracownik")]
    [InlineData(null, "10000000-0000-0000-0000-000000000001", null)]
    public void MapsApplicationRoleByTenantAndOwner(
        string? hubRole,
        string tenantId,
        string? expectedRole)
    {
        Assert.Equal(expectedRole, HubSsoService.MapApplicationRole(hubRole, Guid.Parse(tenantId)));
    }

    [Theory]
    [InlineData("OWNER", "", true)]
    [InlineData("", "MEMBER", true)]
    [InlineData("ADMIN", "MEMBER", true)]
    [InlineData("", "", false)]
    [InlineData("GUEST", "VIEWER", false)]
    public void RequiresRecognizedOrganizationMembership(
        string organizationRole,
        string instanceRole,
        bool expected)
    {
        Assert.Equal(expected, HubSsoService.HasEligibleMembership(organizationRole, instanceRole));
    }

    [Theory]
    [InlineData("owner", false)]
    [InlineData("admin", true)]
    [InlineData("member", true)]
    public void RequiresEmployeeRecordForEveryoneExceptOwner(string hubRole, bool expected)
    {
        Assert.Equal(expected, HubSsoService.RequiresEmployeeRecord(hubRole));
    }
}
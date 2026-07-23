using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.Organization;

public sealed class TenantHubLinkTests
{
    [Fact]
    public void LinkToHub_CanBeRepeatedForTheSameOrganizationAndInstance()
    {
        var tenant = Tenant.Create("Acme", "acme");

        tenant.LinkToHub("org-1", "instance-1");
        tenant.LinkToHub("org-1", "instance-1");

        Assert.Equal("org-1", tenant.HubOrganizationId);
        Assert.Equal("instance-1", tenant.HubProductInstanceId);
    }

    [Fact]
    public void LinkToHub_RejectsChangingTheOrganization()
    {
        var tenant = Tenant.Create("Acme", "acme");
        tenant.LinkToHub("org-1", "instance-1");

        Assert.Throws<InvalidOperationException>(() => tenant.LinkToHub("org-2", "instance-1"));
    }

    [Fact]
    public void LinkToHub_RejectsChangingTheProductInstance()
    {
        var tenant = Tenant.Create("Acme", "acme");
        tenant.LinkToHub("org-1", "instance-1");

        Assert.Throws<InvalidOperationException>(() => tenant.LinkToHub("org-1", "instance-2"));
    }
}
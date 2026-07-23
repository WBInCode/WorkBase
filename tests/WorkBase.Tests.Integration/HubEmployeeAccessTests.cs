using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using WorkBase.Contracts;
using WorkBase.Infrastructure.HubPlatform;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Integration;

public sealed class HubEmployeeAccessTests
{
    private static readonly Guid TenantId =
        Guid.Parse("10000000-0000-0000-0000-000000000010");

    [Fact]
    public async Task Queue_adds_one_request_only_for_a_HUB_tenant()
    {
        await using var db = CreateDbContext();
        var tenant = Tenant.Create("Acme", "acme");
        SetId(tenant, TenantId);
        tenant.LinkToHub("org-acme", "instance-acme");
        db.Add(tenant);
        await db.SaveChangesAsync();
        var queue = new EmployeeAccessProvisioningQueue(db);
        var request = new EmployeeAccessInvitationRequest(
            TenantId, Guid.NewGuid(), "jan@acme.test", "Jan", "Kowalski");

        await queue.QueueInvitationAsync(request);
        await db.SaveChangesAsync();
        await queue.QueueInvitationAsync(request);
        await db.SaveChangesAsync();

        var queued = await db.Set<HubEmployeeAccessRequest>().SingleAsync();
        Assert.Equal("org-acme", queued.HubOrganizationId);
        Assert.Equal(HubEmployeeAccessStatus.Pending, queued.Status);
        Assert.Equal(0, queued.Attempts);
    }

    [Fact]
    public async Task Job_sends_idempotent_invitation_and_marks_it_as_invited()
    {
        await using var db = CreateDbContext();
        var employee = Employee.Create(
            TenantId, "Jan", "Kowalski", "jan@acme.test", null, DateTime.UtcNow);
        var employeeId = employee.Id;
        db.AddRange(employee, HubEmployeeAccessRequest.Create(
            TenantId,
            employeeId,
            "org-acme",
            "instance-acme",
            "jan@acme.test",
            "Jan",
            "Kowalski"));
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(
            HttpStatusCode.Created,
            """{"invitationId":"inv-1","status":"pending"}""");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:Enabled"] = "true",
                ["Hub:BaseUrl"] = "https://hub.test",
                ["Hub:ClientId"] = "workbase",
                ["Hub:ClientSecret"] = "secret",
                ["Hub:EmployeeAccessSyncEnabled"] = "true",
            })
            .Build();
        var job = new HubEmployeeAccessJob(
            db,
            new StubHttpClientFactory(handler),
            configuration,
            NullLogger<HubEmployeeAccessJob>.Instance);

        await job.ExecuteAsync();

        var queued = await db.Set<HubEmployeeAccessRequest>().SingleAsync();
        Assert.Equal(HubEmployeeAccessStatus.Invited, queued.Status);
        Assert.Equal("inv-1", queued.HubInvitationId);
        Assert.Equal($"workbase:invite:{TenantId:N}:{employeeId:N}", handler.IdempotencyKey);
        Assert.Equal(
            "https://hub.test/api/v1/organizations/org-acme/invitations",
            handler.RequestUri?.ToString());
        Assert.Contains("\"externalReference\"", handler.Body);
        Assert.Contains(employeeId.ToString(), handler.Body);
    }

    [Fact]
    public async Task Sso_linker_connects_employee_and_activates_request()
    {
        await using var db = CreateDbContext();
        var employee = Employee.Create(
            TenantId, "Jan", "Kowalski", "jan@acme.test", null, DateTime.UtcNow);
        var accessRequest = HubEmployeeAccessRequest.Create(
            TenantId,
            employee.Id,
            "org-acme",
            "instance-acme",
            employee.Email,
            employee.FirstName,
            employee.LastName);
        db.AddRange(employee, accessRequest);
        await db.SaveChangesAsync();
        var keycloakUserId = Guid.NewGuid();
        var linker = new HubEmployeeIdentityLinker(
            db, NullLogger<HubEmployeeIdentityLinker>.Instance);

        var decision = await linker.ResolveForSsoAsync(TenantId, employee.Email);
        var linked = await linker.LinkOnSsoAsync(
            TenantId, employee.Id, "hub-user-1", keycloakUserId.ToString());

        Assert.Equal(employee.Id, decision.EmployeeId);
        Assert.False(decision.AccessDenied);
        Assert.True(linked);
        Assert.Equal(keycloakUserId, employee.UserId);
        Assert.Equal(HubEmployeeAccessStatus.Active, accessRequest.Status);
        Assert.Equal("hub-user-1", accessRequest.HubUserId);
    }

    [Fact]
    public async Task Sso_linker_denies_an_inactive_employee_before_account_linking()
    {
        await using var db = CreateDbContext();
        var employee = Employee.Create(
            TenantId, "Jan", "Kowalski", "jan@acme.test", null, DateTime.UtcNow);
        employee.Deactivate(DateTime.UtcNow);
        db.Add(employee);
        await db.SaveChangesAsync();
        var linker = new HubEmployeeIdentityLinker(
            db, NullLogger<HubEmployeeIdentityLinker>.Instance);

        var decision = await linker.ResolveForSsoAsync(TenantId, employee.Email);

        Assert.Equal(employee.Id, decision.EmployeeId);
        Assert.True(decision.AccessDenied);
    }

    [Fact]
    public async Task Deactivation_revokes_only_WorkBase_access_idempotently()
    {
        await using var db = CreateDbContext();
        var employeeId = Guid.NewGuid();
        var accessRequest = HubEmployeeAccessRequest.Create(
            TenantId,
            employeeId,
            "org-acme",
            "instance-acme",
            "jan@acme.test",
            "Jan",
            "Kowalski");
        accessRequest.MarkInvited("inv-1", "membership-1", "hub-user-1", membershipActive: true);
        db.Add(accessRequest);
        await db.SaveChangesAsync();
        var queue = new EmployeeAccessProvisioningQueue(db);
        await queue.QueueRevocationAsync(TenantId, employeeId);
        await db.SaveChangesAsync();
        var handler = new RecordingHandler(HttpStatusCode.NotFound, "");
        var job = new HubEmployeeAccessJob(
            db,
            new StubHttpClientFactory(handler),
            CreateHubConfiguration(),
            NullLogger<HubEmployeeAccessJob>.Instance);

        await job.ExecuteAsync();

        Assert.Equal(HubEmployeeAccessStatus.Revoked, accessRequest.Status);
        Assert.Equal(HttpMethod.Delete, handler.Method);
        Assert.Equal($"workbase:revoke:{TenantId:N}:{employeeId:N}", handler.IdempotencyKey);
        Assert.Equal(
            $"https://hub.test/api/v1/organizations/org-acme/product-instances/instance-acme/members/by-external-reference/{employeeId}?email=jan%40acme.test",
            handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task User_access_verifier_reads_current_HUB_role_and_caches_it()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            """{"active":true,"hubUserId":"hub-1","orgRole":"MEMBER","instanceRole":"member"}""");
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var verifier = new HubUserAccessVerifier(
            new StubHttpClientFactory(handler),
            CreateHubConfiguration(),
            memoryCache,
            NullLogger<HubUserAccessVerifier>.Instance);

        var first = await verifier.VerifyAsync("instance-acme", "hub-1", "JAN@ACME.TEST");
        var second = await verifier.VerifyAsync("instance-acme", "hub-1", "jan@acme.test");

        Assert.True(first.Active);
        Assert.Equal("member", first.HubRole);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            "https://hub.test/api/v1/instances/instance-acme/user-access/check",
            handler.RequestUri?.ToString());
        Assert.Contains("jan@acme.test", handler.Body);
        Assert.Contains("hub-1", handler.Body);
    }

    private static WorkBaseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkBaseDbContext>()
            .UseInMemoryDatabase($"hub-employee-access-{Guid.NewGuid():N}")
            .Options;
        return new WorkBaseDbContext(options);
    }

    private static IConfiguration CreateHubConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Hub:Enabled"] = "true",
                ["Hub:BaseUrl"] = "https://hub.test",
                ["Hub:ClientId"] = "workbase",
                ["Hub:ClientSecret"] = "secret",
                ["Hub:EmployeeAccessSyncEnabled"] = "true",
            })
            .Build();

    private static void SetId(Tenant tenant, Guid id)
    {
        typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, id);
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode statusCode, string responseBody)
        : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public string Body { get; private set; } = "";
        public int RequestCount { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            RequestUri = request.RequestUri;
            Method = request.Method;
            IdempotencyKey = request.Headers.TryGetValues("Idempotency-Key", out var values)
                ? values.Single()
                : null;
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
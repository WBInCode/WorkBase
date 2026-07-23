using System.Net.Http.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Organization.Domain.Entities;

namespace WorkBase.Infrastructure.HubPlatform;

public sealed class HubEmployeeAccessJob(
    WorkBaseDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<HubEmployeeAccessJob> logger)
{
    private sealed record HubInvitationResponse(
        string? InvitationId,
        string? MembershipId,
        string? HubUserId,
        string? UserId,
        string? Status);

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync()
    {
        var options = configuration.GetSection(HubOptions.SectionName).Get<HubOptions>() ?? new HubOptions();
        if (!options.Enabled
            || !options.EmployeeAccessSyncEnabled
            || string.IsNullOrWhiteSpace(options.BaseUrl)
            || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            return;
        }

        await QueueMissingActiveEmployeesAsync();

        var now = DateTime.UtcNow;
        var staleProcessingBefore = now.AddMinutes(-30);
        var requests = await dbContext.Set<HubEmployeeAccessRequest>()
            .IgnoreQueryFilters()
            .Where(request =>
                ((request.Status == HubEmployeeAccessStatus.Pending
                                    || request.Status == HubEmployeeAccessStatus.Failed
                                    || request.Status == HubEmployeeAccessStatus.RevocationPending)
                 && request.NextAttemptAt <= now)
                || (request.Status == HubEmployeeAccessStatus.Processing
                    && request.UpdatedAt <= staleProcessingBefore))
            .OrderBy(request => request.NextAttemptAt)
            .Take(25)
            .ToListAsync();

        foreach (var request in requests)
        {
            await ProcessAsync(request, options);
        }
    }

    private async Task QueueMissingActiveEmployeesAsync()
    {
        var missingEmployees = await (
            from employee in dbContext.Set<Employee>().IgnoreQueryFilters()
            join tenant in dbContext.Set<Tenant>().IgnoreQueryFilters()
                on employee.TenantId equals tenant.Id
            where employee.Status == EmployeeStatus.Active
                  && tenant.HubOrganizationId != null
                  && tenant.HubProductInstanceId != null
                  && !dbContext.Set<HubEmployeeAccessRequest>()
                      .IgnoreQueryFilters()
                      .Any(request => request.TenantId == employee.TenantId
                                      && request.EmployeeId == employee.Id)
            orderby employee.CreatedAt
            select new
            {
                Employee = employee,
                HubOrganizationId = tenant.HubOrganizationId!,
                HubProductInstanceId = tenant.HubProductInstanceId!,
            })
            .Take(100)
            .ToListAsync();

        foreach (var item in missingEmployees)
        {
            dbContext.Set<HubEmployeeAccessRequest>().Add(
                HubEmployeeAccessRequest.Create(
                    item.Employee.TenantId,
                    item.Employee.Id,
                    item.HubOrganizationId,
                    item.HubProductInstanceId,
                    item.Employee.Email,
                    item.Employee.FirstName,
                    item.Employee.LastName));
        }

        if (missingEmployees.Count > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation(
                "Queued {Count} existing employees for HUB WorkBase access reconciliation",
                missingEmployees.Count);
        }
    }

    private async Task ProcessAsync(HubEmployeeAccessRequest request, HubOptions options)
    {
        var expectedOperation = request.Operation;
        request.MarkProcessing();
        await dbContext.SaveChangesAsync();

        try
        {
            if (expectedOperation == HubEmployeeAccessOperation.Revoke)
                await SendRevocationAsync(request, options);
            else
                await SendInvitationAsync(request, options);
        }
        catch (Exception ex)
        {
            await dbContext.Entry(request).ReloadAsync();
            if (request.Operation != expectedOperation)
            {
                logger.LogInformation(
                    "HUB access operation for employee {EmployeeId} changed to {Operation}; stale failure ignored",
                    request.EmployeeId, request.Operation);
                return;
            }
            request.MarkFailed(ex.Message);
            await dbContext.SaveChangesAsync();
            logger.LogWarning(ex, "HUB access sync failed for employee {EmployeeId}", request.EmployeeId);
        }
    }

    private async Task SendInvitationAsync(HubEmployeeAccessRequest request, HubOptions options)
    {
        var employee = await dbContext.Set<Employee>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.Id == request.EmployeeId && item.TenantId == request.TenantId);
        if (employee is null || employee.Status != EmployeeStatus.Active)
        {
            request.QueueRevocation();
            await dbContext.SaveChangesAsync();
            return;
        }

        var organizationId = Uri.EscapeDataString(request.HubOrganizationId);
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl.TrimEnd('/')}/api/v1/organizations/{organizationId}/invitations");
        AddHubHeaders(message, options, $"workbase:invite:{request.TenantId:N}:{request.EmployeeId:N}");
        message.Content = JsonContent.Create(new
        {
            email = employee.Email,
            firstName = employee.FirstName,
            lastName = employee.LastName,
            productKey = options.ClientId,
            productInstanceId = request.HubProductInstanceId,
            role = "member",
            externalReference = request.EmployeeId.ToString(),
        });

        using var response = await httpClientFactory.CreateClient("hub-platform").SendAsync(message);
        if (!response.IsSuccessStatusCode)
        {
            await MarkHttpFailureAsync(
                request, response, HubEmployeeAccessOperation.Invite, "invitation");
            return;
        }

        HubInvitationResponse? result = null;
        if (response.Content.Headers.ContentLength is not 0)
            result = await response.Content.ReadFromJsonAsync<HubInvitationResponse>();

        var status = result?.Status?.Trim().ToLowerInvariant();
        await dbContext.Entry(request).ReloadAsync();
        if (request.Operation != HubEmployeeAccessOperation.Invite)
        {
            logger.LogInformation(
                "Employee {EmployeeId} was queued for revocation while invitation was in flight",
                request.EmployeeId);
            return;
        }
        request.MarkInvited(
            result?.InvitationId,
            result?.MembershipId,
            result?.HubUserId ?? result?.UserId,
            membershipActive: status is "active" or "member");
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "HUB invitation synchronized for employee {EmployeeId}; status {Status}",
            request.EmployeeId, request.Status);
    }

    private async Task SendRevocationAsync(HubEmployeeAccessRequest request, HubOptions options)
    {
        var organizationId = Uri.EscapeDataString(request.HubOrganizationId);
        var instanceId = Uri.EscapeDataString(request.HubProductInstanceId);
        var employeeId = Uri.EscapeDataString(request.EmployeeId.ToString());
        var email = Uri.EscapeDataString(request.Email.Trim().ToLowerInvariant());
        using var message = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{options.BaseUrl.TrimEnd('/')}/api/v1/organizations/{organizationId}/product-instances/{instanceId}/members/by-external-reference/{employeeId}?email={email}");
        AddHubHeaders(message, options, $"workbase:revoke:{request.TenantId:N}:{request.EmployeeId:N}");

        using var response = await httpClientFactory.CreateClient("hub-platform").SendAsync(message);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            await MarkHttpFailureAsync(
                request, response, HubEmployeeAccessOperation.Revoke, "revocation");
            return;
        }

        await dbContext.Entry(request).ReloadAsync();
        if (request.Operation != HubEmployeeAccessOperation.Revoke)
            return;
        request.MarkRevoked();
        await dbContext.SaveChangesAsync();
        logger.LogInformation("HUB WorkBase access revoked for employee {EmployeeId}", request.EmployeeId);
    }

    private async Task MarkHttpFailureAsync(
        HubEmployeeAccessRequest request,
        HttpResponseMessage response,
        HubEmployeeAccessOperation expectedOperation,
        string operation)
    {
        var body = await response.Content.ReadAsStringAsync();
        await dbContext.Entry(request).ReloadAsync();
        if (request.Operation != expectedOperation)
            return;
        request.MarkFailed($"HUB returned {(int)response.StatusCode}: {body}");
        await dbContext.SaveChangesAsync();
        logger.LogWarning(
            "HUB {Operation} failed for employee {EmployeeId}: HTTP {StatusCode}",
            operation, request.EmployeeId, (int)response.StatusCode);
    }

    private static void AddHubHeaders(
        HttpRequestMessage message,
        HubOptions options,
        string idempotencyKey)
    {
        message.Headers.Add("x-sso-client-id", options.ClientId);
        message.Headers.Add("x-sso-secret", options.ClientSecret);
        message.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
    }
}
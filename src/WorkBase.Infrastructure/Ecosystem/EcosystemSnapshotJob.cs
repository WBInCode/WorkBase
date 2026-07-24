using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Infrastructure.Ecosystem;

public sealed class EcosystemSnapshotJob(
    WorkBaseDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IOptions<EcosystemOptions> options,
    ILogger<EcosystemSnapshotJob> logger)
{
    private readonly EcosystemOptions _options = options.Value;

    public async Task ExecuteAsync(Guid tenantId, Guid employeeId)
    {
        if (!_options.Enabled || tenantId != _options.TenantId)
            return;

        var employee = await dbContext.Set<Employee>()
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == employeeId);
        if (employee is null || employee.Status == EmployeeStatus.Inactive)
            return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-30);
        var to = today.AddDays(180);
        var schedules = await dbContext.Set<Schedule>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.EmployeeId == employeeId && item.Date >= from && item.Date <= to)
            .OrderBy(item => item.Date)
            .ToListAsync();
        var leaves = await dbContext.Set<LeaveRequest>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId
                && item.EmployeeId == employeeId
                && item.Status == LeaveRequestStatus.Approved
                && item.EndDate >= from.ToDateTime(TimeOnly.MinValue)
                && item.StartDate <= to.ToDateTime(TimeOnly.MaxValue))
            .OrderBy(item => item.StartDate)
            .ToListAsync();
        var leaveTypeIds = leaves.Select(item => item.LeaveTypeId).Distinct().ToArray();
        var leaveTypes = leaveTypeIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Set<LeaveType>()
                .IgnoreQueryFilters()
                .Where(item => item.TenantId == tenantId && leaveTypeIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Name);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
        DateTime ToUtc(DateOnly date, TimeOnly time)
            => TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified),
                timeZone);

        var events = new List<object>();
        events.AddRange(schedules.Select(schedule =>
        {
            var endDate = schedule.PlannedEnd > schedule.PlannedStart ? schedule.Date : schedule.Date.AddDays(1);
            return (object)new
            {
                sourceRef = $"schedule:{schedule.Id}",
                title = string.IsNullOrWhiteSpace(schedule.ShiftType)
                    ? "WorkBase: Grafik pracy"
                    : $"WorkBase: {schedule.ShiftType}",
                start = ToUtc(schedule.Date, schedule.PlannedStart),
                end = ToUtc(endDate, schedule.PlannedEnd),
                allDay = false
            };
        }));
        events.AddRange(leaves.Select(leave =>
        {
            var startDate = DateOnly.FromDateTime(leave.StartDate);
            var endDateExclusive = DateOnly.FromDateTime(leave.EndDate).AddDays(1);
            return (object)new
            {
                sourceRef = $"leave:{leave.Id}",
                title = $"WorkBase: {leaveTypes.GetValueOrDefault(leave.LeaveTypeId, "Nieobecność")}",
                start = ToUtc(startDate, TimeOnly.MinValue),
                end = ToUtc(endDateExclusive, TimeOnly.MinValue),
                allDay = true
            };
        }));

        var client = httpClientFactory.CreateClient("RytmEcosystem");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/ecosystem/ingest")
        {
            Content = JsonContent.Create(new
            {
                source = "WORKBASE",
                userEmail = employee.Email,
                snapshotAt = DateTime.UtcNow,
                hubOrgId = _options.HubOrgId,
                events
            })
        };
        request.Headers.TryAddWithoutValidation("x-ecosystem-secret", _options.Secret);
        using var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Rytm snapshot failed for employee {EmployeeId}: HTTP {Status} {Body}",
                employeeId, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }
}
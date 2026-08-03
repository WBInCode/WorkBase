using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkBase.Infrastructure.Persistence;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Modules.Organization.Domain.Entities;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using TaskPriority = WorkBase.Modules.Tasks.Domain.Entities.TaskPriority;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

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
                allDay = false,
                // Grafik mowi "jestem w firmie", a nie "nie przeszkadzac" — inaczej Rytm
                // pokazywalby kazdego pracujacego jako zajetego przez cala zmiane.
                busy = false
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
                allDay = true,
                busy = true
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
                events,
                tasks = await BuildTasksAsync(tenantId, employeeId)
            })
        };
        request.Headers.TryAddWithoutValidation("x-ecosystem-secret", _options.Secret);
        using var response = await client.SendAsync(request);

        // Rytm nie zna tego adresu, czyli pracownik po prostu nie ma tam konta. To stan
        // trwaly, a nie usterka: ponawianie nigdy nie zakonczy sie powodzeniem. Zanim to
        // rozroznilismy, piecioro pracownikow bez konta generowalo ponad 5000 nieudanych
        // wywolan na dobe, 1493 zablokowane zadania i 35 MB w tabelach kolejki — a prawdziwe
        // awarie ginely w tym szumie.
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                "Pomijam synchronizacje z Rytmem dla pracownika {EmployeeId}: brak konta w Rytmie.",
                employeeId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Rytm snapshot failed for employee {EmployeeId}: HTTP {Status} {Body}",
                employeeId, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Zadania pracownika w postaci, ktora rozumie Rytm. Wysylamy wszystkie otwarte
    /// oraz zamkniete z ostatnich kilku tygodni: Rytm anuluje u siebie to, czego tu nie ma,
    /// wiec zbyt waskie okno kasowaloby swiezo zamkniete zadania zamiast oznaczyc je jako zrobione.
    /// </summary>
    private async Task<List<object>> BuildTasksAsync(Guid tenantId, Guid employeeId)
    {
        var closedSince = DateTime.UtcNow.AddDays(-Math.Max(1, _options.ClosedTaskWindowDays));

        var statuses = await dbContext.Set<TaskStatus>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId)
            .ToDictionaryAsync(item => item.Id, item => item);
        var priorities = await dbContext.Set<TaskPriority>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId)
            .ToDictionaryAsync(item => item.Id, item => item);

        var finalStatusIds = statuses.Values.Where(item => item.IsFinal).Select(item => item.Id).ToHashSet();

        var items = await dbContext.Set<TaskItem>()
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId
                && item.AssigneeId == employeeId
                && (!finalStatusIds.Contains(item.StatusId)
                    || (item.CompletedAt != null && item.CompletedAt >= closedSince)))
            .OrderBy(item => item.DueDate ?? DateTime.MaxValue)
            .Take(500)
            .ToListAsync();

        return items.Select(item =>
        {
            var status = statuses.GetValueOrDefault(item.StatusId);
            var priority = priorities.GetValueOrDefault(item.PriorityId);
            return (object)new
            {
                sourceRef = item.Id.ToString(),
                title = item.Title,
                notes = item.Description,
                status = MapStatus(status),
                priority = MapPriority(priority),
                dueDate = item.DueDate,
                completedAt = item.CompletedAt,
                url = string.IsNullOrWhiteSpace(_options.AppUrl)
                    ? null
                    : $"{_options.AppUrl.TrimEnd('/')}/tasks/{item.Id}"
            };
        }).ToList();
    }

    private static string MapStatus(TaskStatus? status)
    {
        if (status is null) return "PLANNED";
        if (status.IsFinal) return "DONE";
        return status.Code.Equals("IN_PROGRESS", StringComparison.OrdinalIgnoreCase)
            ? "IN_PROGRESS"
            : "PLANNED";
    }

    // Kody priorytetow sa konfigurowalne per najemca, wiec gdy kod jest nieznany,
    // opieramy sie na kolejnosci sortowania — to jedyna informacja, ktora zawsze istnieje.
    private static string MapPriority(TaskPriority? priority)
    {
        if (priority is null) return "MEDIUM";
        return priority.Code.ToUpperInvariant() switch
        {
            "LOW" => "LOW",
            "NORMAL" or "MEDIUM" => "MEDIUM",
            "HIGH" or "CRITICAL" or "URGENT" => "HIGH",
            _ => priority.SortOrder <= 1 ? "LOW" : priority.SortOrder >= 3 ? "HIGH" : "MEDIUM",
        };
    }
}
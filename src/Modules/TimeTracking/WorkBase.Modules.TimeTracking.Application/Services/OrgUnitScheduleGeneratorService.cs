using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkBase.Contracts;
using WorkBase.Modules.TimeTracking.Application.Commands;
using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Domain.Entities;

namespace WorkBase.Modules.TimeTracking.Application.Services;

public sealed class OrgUnitScheduleGeneratorService(
    IOrgUnitScheduleRepository orgUnitScheduleRepository,
    IScheduleRepository scheduleRepository,
    IOrganizationLookupService organizationLookupService,
    ILogger<OrgUnitScheduleGeneratorService> logger)
{
    private const int DefaultWeeksAhead = 4;

    public async Task GenerateForOrgUnitAsync(
        Guid tenantId,
        Guid orgUnitScheduleId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var orgUnitSchedule = await orgUnitScheduleRepository.GetByIdAsync(tenantId, orgUnitScheduleId, cancellationToken);
        if (orgUnitSchedule is null || !orgUnitSchedule.IsActive)
        {
            logger.LogWarning("OrgUnitSchedule {Id} not found or inactive", orgUnitScheduleId);
            return;
        }

        var from = fromDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var to = toDate ?? from.AddDays(DefaultWeeksAhead * 7);

        // Get employees for this org unit
        var employeeIds = await organizationLookupService.GetEmployeeIdsByOrgUnitAsync(
            tenantId, orgUnitSchedule.OrgUnitId, cancellationToken);

        if (employeeIds.Count == 0)
        {
            logger.LogInformation("No employees in org unit {OrgUnitId}, skipping generation", orgUnitSchedule.OrgUnitId);
            return;
        }

        // Parse week pattern
        var weekPattern = DeserializeWeekPattern(orgUnitSchedule.WeekPattern);
        if (weekPattern.Count == 0)
        {
            logger.LogWarning("Empty week pattern for OrgUnitSchedule {Id}", orgUnitScheduleId);
            return;
        }

        var patternByDay = weekPattern
            .GroupBy(p => p.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        // Remove existing OrgUnit-generated entries for this schedule (future only)
        var existingOrgUnitEntries = await scheduleRepository.GetByOrgUnitScheduleIdAsync(
            tenantId, orgUnitScheduleId, from, cancellationToken);

        if (existingOrgUnitEntries.Count > 0)
            scheduleRepository.RemoveRange(existingOrgUnitEntries);

        // Get all existing Individual/Unplanned entries for these employees in range
        var existingEntries = await scheduleRepository.GetByEmployeesDateRangeAsync(
            tenantId, employeeIds, from, to, cancellationToken);

        var individualKeys = existingEntries
            .Where(s => s.Source != ScheduleSource.OrgUnit)
            .Select(s => (s.EmployeeId, s.Date))
            .ToHashSet();

        // Generate new entries
        var newSchedules = new List<Schedule>();
        var current = from;

        while (current <= to)
        {
            if (patternByDay.TryGetValue(current.DayOfWeek, out var pattern))
            {
                foreach (var employeeId in employeeIds)
                {
                    // Never overwrite Individual or Unplanned entries
                    if (individualKeys.Contains((employeeId, current)))
                        continue;

                    newSchedules.Add(Schedule.Create(
                        tenantId,
                        employeeId,
                        current,
                        pattern.PlannedStart,
                        pattern.PlannedEnd,
                        pattern.ShiftType,
                        pattern.TemplateId,
                        ScheduleSource.OrgUnit,
                        orgUnitScheduleId));
                }
            }
            current = current.AddDays(1);
        }

        if (newSchedules.Count > 0)
            await scheduleRepository.AddManyAsync(newSchedules, cancellationToken);

        logger.LogInformation(
            "Generated {Count} schedule entries for OrgUnitSchedule {Id} ({From} to {To})",
            newSchedules.Count, orgUnitScheduleId, from, to);
    }

    public async Task RegenerateForEmployeeAsync(
        Guid tenantId,
        Guid employeeId,
        Guid orgUnitId,
        CancellationToken cancellationToken = default)
    {
        var orgUnitSchedule = await orgUnitScheduleRepository.GetByOrgUnitIdAsync(tenantId, orgUnitId, cancellationToken);

        // If this org unit has no schedule, try ancestors
        if (orgUnitSchedule is null)
        {
            var ancestors = await organizationLookupService.GetAncestorOrgUnitIdsAsync(orgUnitId, cancellationToken);
            foreach (var ancestorId in ancestors)
            {
                orgUnitSchedule = await orgUnitScheduleRepository.GetByOrgUnitIdAsync(tenantId, ancestorId, cancellationToken);
                if (orgUnitSchedule is not null)
                    break;
            }
        }

        if (orgUnitSchedule is null || !orgUnitSchedule.IsActive)
            return;

        var from = DateOnly.FromDateTime(DateTime.UtcNow);
        var to = from.AddDays(DefaultWeeksAhead * 7);

        var weekPattern = DeserializeWeekPattern(orgUnitSchedule.WeekPattern);
        var patternByDay = weekPattern
            .GroupBy(p => p.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.First());

        // Get existing entries for this employee
        var existing = await scheduleRepository.GetByDateRangeAsync(tenantId, employeeId, from, to, cancellationToken);

        // Remove old OrgUnit entries for this employee
        var oldOrgUnitEntries = existing.Where(s => s.Source == ScheduleSource.OrgUnit).ToList();
        if (oldOrgUnitEntries.Count > 0)
            scheduleRepository.RemoveRange(oldOrgUnitEntries);

        var individualDates = existing
            .Where(s => s.Source != ScheduleSource.OrgUnit)
            .Select(s => s.Date)
            .ToHashSet();

        var newSchedules = new List<Schedule>();
        var current = from;

        while (current <= to)
        {
            if (patternByDay.TryGetValue(current.DayOfWeek, out var pattern) && !individualDates.Contains(current))
            {
                newSchedules.Add(Schedule.Create(
                    tenantId,
                    employeeId,
                    current,
                    pattern.PlannedStart,
                    pattern.PlannedEnd,
                    pattern.ShiftType,
                    pattern.TemplateId,
                    ScheduleSource.OrgUnit,
                    orgUnitSchedule.Id));
            }
            current = current.AddDays(1);
        }

        if (newSchedules.Count > 0)
            await scheduleRepository.AddManyAsync(newSchedules, cancellationToken);
    }

    private static List<DayShiftPattern> DeserializeWeekPattern(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<DayShiftPattern>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

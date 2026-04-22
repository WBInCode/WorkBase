using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Modules.TimeTracking.Application.Dtos;
using WorkBase.Modules.TimeTracking.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Queries;

public sealed class GetBreakAvailabilityHandler(
    IBreakPolicyRepository breakPolicyRepository,
    ITimeEntryRepository timeEntryRepository)
    : IQueryHandler<GetBreakAvailabilityQuery, BreakAvailabilityDto>
{
    private static readonly Dictionary<BreakType, string> Labels = new()
    {
        [BreakType.Paid] = "Płatna",
        [BreakType.Unpaid] = "Bezpłatna",
    };

    public async Task<Result<BreakAvailabilityDto>> Handle(
        GetBreakAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var policies = await breakPolicyRepository.GetByTenantAsync(request.TenantId, cancellationToken);
        var activePolicies = policies.Where(p => p.IsActive).ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayEntries = await timeEntryRepository.GetEntriesForDateAsync(
            request.TenantId, request.EmployeeId, today, cancellationToken);

        var options = new List<BreakOptionDto>();

        // If no policies at all, allow both types with no limits
        if (activePolicies.Count == 0)
        {
            foreach (var bt in new[] { BreakType.Paid, BreakType.Unpaid })
            {
                var used = todayEntries.Count(e => e.Type == TimeEntryType.BreakStart && e.BreakType == bt);
                var usedMin = CalculateBreakMinutes(todayEntries, bt);
                options.Add(new BreakOptionDto(
                    bt.ToString(), Labels[bt], true,
                    used, null, usedMin, null, null, null));
            }
        }
        else
        {
            foreach (var policy in activePolicies)
            {
                var bt = policy.BreakType;
                var used = todayEntries.Count(e => e.Type == TimeEntryType.BreakStart && e.BreakType == bt);
                var usedMin = CalculateBreakMinutes(todayEntries, bt);

                var available = true;
                string? denial = null;

                if (policy.MaxPerDay.HasValue && used >= policy.MaxPerDay.Value)
                {
                    available = false;
                    denial = $"Limit {policy.MaxPerDay.Value} przerw na dzień osiągnięty";
                }
                else if (policy.MaxMinutesPerDay.HasValue && usedMin >= policy.MaxMinutesPerDay.Value)
                {
                    available = false;
                    denial = $"Limit {policy.MaxMinutesPerDay.Value} min na dzień osiągnięty";
                }

                options.Add(new BreakOptionDto(
                    bt.ToString(), Labels[bt], available,
                    used, policy.MaxPerDay, usedMin,
                    policy.MaxMinutesPerDay, policy.MaxMinutesPerBreak, denial));
            }
        }

        return new BreakAvailabilityDto(options);
    }

    private static double CalculateBreakMinutes(List<TimeEntry> entries, BreakType breakType)
    {
        var ordered = entries.OrderBy(e => e.EntryTime).ToList();
        var total = TimeSpan.Zero;
        DateTime? breakStart = null;

        foreach (var e in ordered)
        {
            if (e.Type == TimeEntryType.BreakStart && e.BreakType == breakType)
                breakStart = e.EntryTime;
            else if (e.Type == TimeEntryType.BreakEnd && breakStart.HasValue)
            {
                total += e.EntryTime - breakStart.Value;
                breakStart = null;
            }
        }

        if (breakStart.HasValue)
            total += DateTime.UtcNow - breakStart.Value;

        return Math.Round(total.TotalMinutes, 1);
    }
}

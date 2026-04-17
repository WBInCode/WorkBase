using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Services;

/// <summary>
/// Calculates leave balance proportionally to hire date and validates leave requests.
/// </summary>
public interface ILeaveBalanceCalculator
{
    /// <summary>
    /// Calculates proportional days for a year based on hire date.
    /// If hired before the year, returns full days. If hired during the year, pro-rata.
    /// </summary>
    decimal CalculateProportionalDays(int daysPerYear, DateTime hireDate, int year);

    /// <summary>
    /// Validates that the employee has enough remaining balance for the request.
    /// </summary>
    Result ValidateBalance(LeaveBalance balance, decimal requestedDays);
}

public sealed class LeaveBalanceCalculator : ILeaveBalanceCalculator
{
    public decimal CalculateProportionalDays(int daysPerYear, DateTime hireDate, int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);

        // Hired before or at the start of the year — full entitlement
        if (hireDate <= yearStart)
            return daysPerYear;

        // Hired after the end of the year — zero
        if (hireDate > yearEnd)
            return 0;

        // Hired during the year — proportional (remaining months / 12, rounded up to 0.5)
        var monthsRemaining = 12 - hireDate.Month + 1;
        if (hireDate.Day > 1)
            monthsRemaining--; // Partial month doesn't count as full month

        // If started on 1st of month, count that month fully
        if (hireDate.Day == 1)
        {
            // monthsRemaining is already correct
        }

        var proportional = (decimal)daysPerYear * monthsRemaining / 12m;

        // Round up to nearest 0.5
        return Math.Ceiling(proportional * 2) / 2;
    }

    public Result ValidateBalance(LeaveBalance balance, decimal requestedDays)
    {
        if (balance.RemainingDays < requestedDays)
            return Result.Failure(new Error("Leave.InsufficientBalance",
                $"Niewystarczający limit urlopowy. Dostępne: {balance.RemainingDays:F1} dni, wnioskowane: {requestedDays:F1} dni."));

        return Result.Success();
    }
}

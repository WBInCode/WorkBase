using WorkBase.Modules.Leave.Application.Services;
using WorkBase.Modules.Leave.Domain.Entities;
using Xunit;

namespace WorkBase.Tests.Unit.Leave;

public class LeaveBalanceCalculatorTests
{
    private readonly LeaveBalanceCalculator _calculator = new();

    [Fact]
    public void CalculateProportionalDays_HiredBeforeYear_ReturnsFullDays()
    {
        var result = _calculator.CalculateProportionalDays(26, new DateTime(2024, 3, 1), 2025);
        Assert.Equal(26, result);
    }

    [Fact]
    public void CalculateProportionalDays_HiredAfterYear_ReturnsZero()
    {
        var result = _calculator.CalculateProportionalDays(26, new DateTime(2026, 5, 1), 2025);
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateProportionalDays_HiredJuly1_ReturnsHalf()
    {
        // July 1 → 6 months remaining → 26 * 6/12 = 13
        var result = _calculator.CalculateProportionalDays(26, new DateTime(2025, 7, 1), 2025);
        Assert.Equal(13, result);
    }

    [Fact]
    public void CalculateProportionalDays_HiredMidMonth_RoundsUpToHalfDay()
    {
        // Hired Oct 15 → 2 full months (Nov, Dec) → 26 * 2/12 = 4.333 → ceil to 4.5
        var result = _calculator.CalculateProportionalDays(26, new DateTime(2025, 10, 15), 2025);
        Assert.Equal(4.5m, result);
    }

    [Fact]
    public void ValidateBalance_SufficientBalance_ReturnsSuccess()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2025, 26);
        var result = _calculator.ValidateBalance(balance, 5);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateBalance_InsufficientBalance_ReturnsFailure()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2025, 5);
        var result = _calculator.ValidateBalance(balance, 10);
        Assert.True(result.IsFailure);
        Assert.Equal("Leave.InsufficientBalance", result.Error.Code);
    }

    [Fact]
    public void ValidateBalance_AccountsForPendingDays()
    {
        var balance = LeaveBalance.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 2025, 10);
        balance.AddPending(8);
        // Remaining = 10 - 0 - 8 = 2
        var result = _calculator.ValidateBalance(balance, 3);
        Assert.True(result.IsFailure);
    }
}

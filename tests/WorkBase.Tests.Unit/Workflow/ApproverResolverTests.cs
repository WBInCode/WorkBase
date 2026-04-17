using NSubstitute;
using WorkBase.Contracts;
using WorkBase.Modules.Workflow.Application.Services;
using Xunit;

namespace WorkBase.Tests.Unit.Workflow;

public class ApproverResolverTests
{
    private readonly ISupervisorLookupService _supervisorLookup = Substitute.For<ISupervisorLookupService>();
    private readonly ApproverResolver _resolver;

    public ApproverResolverTests()
    {
        _resolver = new ApproverResolver(_supervisorLookup);
    }

    [Fact]
    public async Task Supervisor_ReturnsApproverWhenSupervisorExists()
    {
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();

        _supervisorLookup.GetEmployeeIdByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(employeeId);
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns(supervisorId);

        var result = await _resolver.ResolveApproverAsync("supervisor", userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(supervisorId, result.Value);
    }

    [Fact]
    public async Task Supervisor_ReturnsFailureWhenEmployeeNotFound()
    {
        var userId = Guid.NewGuid();
        _supervisorLookup.GetEmployeeIdByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _resolver.ResolveApproverAsync("supervisor", userId);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.EmployeeNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Supervisor_ReturnsFailureWhenSupervisorNotFound()
    {
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        _supervisorLookup.GetEmployeeIdByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(employeeId);
        _supervisorLookup.GetSupervisorEmployeeIdAsync(employeeId, Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var result = await _resolver.ResolveApproverAsync("supervisor", userId);

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.SupervisorNotFound", result.Error.Code);
    }

    [Fact]
    public async Task UnknownStrategy_ReturnsFailure()
    {
        var result = await _resolver.ResolveApproverAsync("nonexistent", Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal("Approval.UnknownStrategy", result.Error.Code);
    }
}

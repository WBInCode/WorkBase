using NSubstitute;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Services;
using WorkBase.Shared.Domain;
using Xunit;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Unit.Tasks;

public class TaskStatusMachineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FromStatusId = Guid.NewGuid();
    private static readonly Guid ToStatusId = Guid.NewGuid();

    private readonly ITaskStatusTransitionRepository _transitionRepository = Substitute.For<ITaskStatusTransitionRepository>();
    private readonly ITaskStatusRepository _statusRepository = Substitute.For<ITaskStatusRepository>();
    private readonly TaskStatusMachine _sut;

    public TaskStatusMachineTests()
    {
        _sut = new TaskStatusMachine(_transitionRepository, _statusRepository);
    }

    // --- ValidateTransitionAsync ---

    [Fact]
    public async Task ValidateTransition_SameStatus_ReturnsFailure()
    {
        var statusId = Guid.NewGuid();

        var result = await _sut.ValidateTransitionAsync(TenantId, statusId, statusId);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.SameStatus", result.Error.Code);
    }

    [Fact]
    public async Task ValidateTransition_ToStatusNotFound_ReturnsNotFound()
    {
        _statusRepository.GetByIdAsync(ToStatusId, Arg.Any<CancellationToken>())
            .Returns((TaskStatus?)null);

        var result = await _sut.ValidateTransitionAsync(TenantId, FromStatusId, ToStatusId);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.StatusNotFound", result.Error.Code);
    }

    [Fact]
    public async Task ValidateTransition_ToStatusInactive_ReturnsFailure()
    {
        var inactiveStatus = CreateStatus(ToStatusId, isActive: false);
        _statusRepository.GetByIdAsync(ToStatusId, Arg.Any<CancellationToken>())
            .Returns(inactiveStatus);

        var result = await _sut.ValidateTransitionAsync(TenantId, FromStatusId, ToStatusId);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.StatusInactive", result.Error.Code);
    }

    [Fact]
    public async Task ValidateTransition_TransitionNotAllowed_ReturnsFailure()
    {
        var activeStatus = CreateStatus(ToStatusId, isActive: true);
        _statusRepository.GetByIdAsync(ToStatusId, Arg.Any<CancellationToken>())
            .Returns(activeStatus);
        _transitionRepository.IsTransitionAllowedAsync(TenantId, FromStatusId, ToStatusId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.ValidateTransitionAsync(TenantId, FromStatusId, ToStatusId);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.TransitionNotAllowed", result.Error.Code);
    }

    [Fact]
    public async Task ValidateTransition_Allowed_ReturnsSuccess()
    {
        var activeStatus = CreateStatus(ToStatusId, isActive: true);
        _statusRepository.GetByIdAsync(ToStatusId, Arg.Any<CancellationToken>())
            .Returns(activeStatus);
        _transitionRepository.IsTransitionAllowedAsync(TenantId, FromStatusId, ToStatusId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.ValidateTransitionAsync(TenantId, FromStatusId, ToStatusId);

        Assert.True(result.IsSuccess);
    }

    // --- Helpers ---

    private static TaskStatus CreateStatus(Guid id, bool isActive = true, bool isFinal = false)
    {
        var status = (TaskStatus)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TaskStatus));
        SetProperty(status, nameof(TaskStatus.Id), id);
        SetProperty(status, nameof(TaskStatus.TenantId), TenantId);
        SetProperty(status, nameof(TaskStatus.Code), "test");
        SetProperty(status, nameof(TaskStatus.Name), "Test Status");
        SetProperty(status, nameof(TaskStatus.IsActive), isActive);
        SetProperty(status, nameof(TaskStatus.IsFinal), isFinal);
        SetProperty(status, nameof(TaskStatus.IsDefault), false);
        SetProperty(status, nameof(TaskStatus.SortOrder), 1);
        return status;
    }

    private static void SetProperty<T>(object obj, string propertyName, T value)
    {
        var prop = obj.GetType().GetProperty(propertyName);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(obj, value);
            return;
        }
        // Try backing field
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(obj, value);
    }
}

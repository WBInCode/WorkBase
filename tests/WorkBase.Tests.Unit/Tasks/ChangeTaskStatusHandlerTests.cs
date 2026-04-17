using NSubstitute;
using WorkBase.Modules.Tasks.Application.Commands;
using WorkBase.Modules.Tasks.Application.Contracts;
using WorkBase.Modules.Tasks.Application.Services;
using WorkBase.Modules.Tasks.Domain.Entities;
using WorkBase.Shared.Domain;
using Xunit;
using TaskStatus = WorkBase.Modules.Tasks.Domain.Entities.TaskStatus;

namespace WorkBase.Tests.Unit.Tasks;

public class ChangeTaskStatusHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid OldStatusId = Guid.NewGuid();
    private static readonly Guid NewStatusId = Guid.NewGuid();

    private readonly ITaskItemRepository _taskRepository = Substitute.For<ITaskItemRepository>();
    private readonly ITaskStatusMachine _statusMachine = Substitute.For<ITaskStatusMachine>();
    private readonly ITaskStatusRepository _statusRepository = Substitute.For<ITaskStatusRepository>();
    private readonly ITaskHistoryRepository _historyRepository = Substitute.For<ITaskHistoryRepository>();
    private readonly ChangeTaskStatusHandler _sut;

    public ChangeTaskStatusHandlerTests()
    {
        _sut = new ChangeTaskStatusHandler(
            _taskRepository, _statusMachine, _statusRepository, _historyRepository);
    }

    [Fact]
    public async Task Handle_TaskNotFound_ReturnsFailure()
    {
        _taskRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var command = new ChangeTaskStatusCommand(TaskId, NewStatusId, UserId) { TenantId = TenantId };
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Handle_TransitionNotAllowed_ReturnsFailure()
    {
        var task = CreateTask(TaskId, OldStatusId);
        _taskRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(task);
        _statusMachine.ValidateTransitionAsync(TenantId, OldStatusId, NewStatusId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Task.TransitionNotAllowed", "Not allowed")));

        var command = new ChangeTaskStatusCommand(TaskId, NewStatusId, UserId) { TenantId = TenantId };
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Task.TransitionNotAllowed", result.Error.Code);
    }

    [Fact]
    public async Task Handle_ValidTransition_UpdatesTaskAndRecordsHistory()
    {
        var task = CreateTask(TaskId, OldStatusId);
        _taskRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(task);
        _statusMachine.ValidateTransitionAsync(TenantId, OldStatusId, NewStatusId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var nonFinalStatus = CreateStatus(NewStatusId, isFinal: false);
        _statusRepository.GetByIdAsync(NewStatusId, Arg.Any<CancellationToken>())
            .Returns(nonFinalStatus);

        var command = new ChangeTaskStatusCommand(TaskId, NewStatusId, UserId) { TenantId = TenantId };
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(NewStatusId, task.StatusId);
        Assert.Null(task.CompletedAt);
        _taskRepository.Received(1).Update(task);
        await _historyRepository.Received(1).AddAsync(Arg.Any<TaskHistoryEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FinalStatus_SetsCompletedAt()
    {
        var task = CreateTask(TaskId, OldStatusId);
        _taskRepository.GetByIdAsync(TaskId, Arg.Any<CancellationToken>())
            .Returns(task);
        _statusMachine.ValidateTransitionAsync(TenantId, OldStatusId, NewStatusId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        var finalStatus = CreateStatus(NewStatusId, isFinal: true);
        _statusRepository.GetByIdAsync(NewStatusId, Arg.Any<CancellationToken>())
            .Returns(finalStatus);

        var command = new ChangeTaskStatusCommand(TaskId, NewStatusId, UserId) { TenantId = TenantId };
        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(task.CompletedAt);
    }

    // --- Helpers ---

    private static TaskItem CreateTask(Guid taskId, Guid statusId)
    {
        var task = TaskItem.Create(TenantId, "Test Task", statusId, Guid.NewGuid(), Guid.NewGuid());
        SetProperty(task, nameof(TaskItem.Id), taskId);
        return task;
    }

    private static TaskStatus CreateStatus(Guid id, bool isFinal = false)
    {
        var status = (TaskStatus)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TaskStatus));
        SetProperty(status, nameof(TaskStatus.Id), id);
        SetProperty(status, nameof(TaskStatus.TenantId), TenantId);
        SetProperty(status, nameof(TaskStatus.Code), "test");
        SetProperty(status, nameof(TaskStatus.Name), "Test");
        SetProperty(status, nameof(TaskStatus.IsActive), true);
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
        var field = obj.GetType().GetField($"<{propertyName}>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(obj, value);
    }
}

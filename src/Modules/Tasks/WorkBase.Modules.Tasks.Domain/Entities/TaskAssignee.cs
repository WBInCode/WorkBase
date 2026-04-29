namespace WorkBase.Modules.Tasks.Domain.Entities;

public sealed class TaskAssignee
{
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid EmployeeId { get; private set; }

    private TaskAssignee() { }

    public static TaskAssignee Create(Guid taskId, Guid employeeId)
        => new() { Id = Guid.NewGuid(), TaskId = taskId, EmployeeId = employeeId };
}

using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record DeleteLeaveTypeCommand(Guid LeaveTypeId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteLeaveTypeHandler(ILeaveTypeRepository repository)
    : ICommandHandler<DeleteLeaveTypeCommand>
{
    public async Task<Result> Handle(DeleteLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await repository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (type is null || type.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("LeaveType.NotFound", "Typ nieobecności nie został znaleziony."));

        repository.Remove(type);
        return Result.Success();
    }
}

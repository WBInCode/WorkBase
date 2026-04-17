using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;
using WorkBase.Modules.Leave.Domain.Entities;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record CreateLeaveTypeCommand(
    string Code, string Name, string? Description, bool IsPaid,
    bool RequiresApproval, int DefaultDaysPerYear, string? Color, int SortOrder)
    : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateLeaveTypeHandler(ILeaveTypeRepository repository) : ICommandHandler<CreateLeaveTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var type = LeaveType.Create(
            request.TenantId, request.Code, request.Name,
            request.IsPaid, request.RequiresApproval, request.DefaultDaysPerYear,
            request.Description, request.Color, request.SortOrder);

        await repository.AddAsync(type, cancellationToken);
        return type.Id;
    }
}

public sealed record UpdateLeaveTypeCommand(
    Guid LeaveTypeId, string Code, string Name, string? Description, bool IsPaid,
    bool RequiresApproval, int DefaultDaysPerYear, string? Color, int SortOrder)
    : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateLeaveTypeHandler(ILeaveTypeRepository repository) : ICommandHandler<UpdateLeaveTypeCommand>
{
    public async Task<Result> Handle(UpdateLeaveTypeCommand request, CancellationToken cancellationToken)
    {
        var type = await repository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (type is null || type.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("LeaveType.NotFound", "Leave type not found"));

        type.Update(request.Name, request.Description, request.IsPaid,
            request.RequiresApproval, request.DefaultDaysPerYear, request.Color, request.SortOrder);
        return Result.Success();
    }
}

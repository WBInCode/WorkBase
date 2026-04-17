using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Modules.Leave.Domain.Entities;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record CreateLeavePolicyCommand(
    Guid LeaveTypeId, string Name, int DaysPerYear, bool AllowCarryOver,
    int MaxCarryOverDays, int? MaxConsecutiveDays, int MinNoticeDays) : ICommand<Guid>, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class CreateLeavePolicyHandler(ILeavePolicyRepository repository) : ICommandHandler<CreateLeavePolicyCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = LeavePolicy.Create(
            request.TenantId, request.LeaveTypeId, request.Name, request.DaysPerYear,
            request.AllowCarryOver, request.MaxCarryOverDays, request.MaxConsecutiveDays, request.MinNoticeDays);

        await repository.AddAsync(policy, cancellationToken);
        return policy.Id;
    }
}

public sealed record UpdateLeavePolicyCommand(
    Guid PolicyId, string Name, int DaysPerYear, bool AllowCarryOver,
    int MaxCarryOverDays, int? MaxConsecutiveDays, int MinNoticeDays) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class UpdateLeavePolicyHandler(ILeavePolicyRepository repository) : ICommandHandler<UpdateLeavePolicyCommand>
{
    public async Task<Result> Handle(UpdateLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(request.PolicyId, cancellationToken);
        if (policy is null || policy.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("LeavePolicy.NotFound", "Leave policy not found"));

        policy.Update(request.Name, request.DaysPerYear, request.AllowCarryOver,
            request.MaxCarryOverDays, request.MaxConsecutiveDays, request.MinNoticeDays);
        return Result.Success();
    }
}

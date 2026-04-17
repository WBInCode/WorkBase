using WorkBase.Modules.Leave.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.Leave.Application.Commands;

public sealed record DeleteLeavePolicyCommand(Guid PolicyId) : ICommand, ITenantRequest
{
    public Guid TenantId { get; set; }
}

public sealed class DeleteLeavePolicyHandler(ILeavePolicyRepository repository)
    : ICommandHandler<DeleteLeavePolicyCommand>
{
    public async Task<Result> Handle(DeleteLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await repository.GetByIdAsync(request.PolicyId, cancellationToken);
        if (policy is null || policy.TenantId != request.TenantId)
            return Result.Failure(Error.NotFound("LeavePolicy.NotFound", "Polityka urlopowa nie została znaleziona."));

        repository.Remove(policy);
        return Result.Success();
    }
}

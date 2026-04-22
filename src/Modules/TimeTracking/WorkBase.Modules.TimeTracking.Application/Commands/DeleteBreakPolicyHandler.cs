using WorkBase.Modules.TimeTracking.Application.Contracts;
using WorkBase.Shared.Cqrs;
using WorkBase.Shared.Domain;

namespace WorkBase.Modules.TimeTracking.Application.Commands;

public sealed class DeleteBreakPolicyHandler(IBreakPolicyRepository breakPolicyRepository)
    : ICommandHandler<DeleteBreakPolicyCommand>
{
    public async Task<Result> Handle(DeleteBreakPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await breakPolicyRepository.GetByIdAsync(
            request.TenantId, request.PolicyId, cancellationToken);

        if (policy is null)
            return Result.Failure(Error.NotFound(
                "BreakPolicy.NotFound",
                "Nie znaleziono polityki przerw."));

        breakPolicyRepository.Remove(policy);

        return Result.Success();
    }
}
